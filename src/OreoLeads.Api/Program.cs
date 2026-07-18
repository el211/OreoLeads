using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OreoLeads.Api.HealthChecks;
using OreoLeads.Api.Middleware;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Infrastructure.Configuration;
using OreoLeads.Infrastructure.Extensions;
using OreoLeads.Infrastructure.Identity;
using OreoLeads.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("Starting OreoLeads API...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Startup config validation ─────────────────────────────────────────────
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var jwtSecret = jwtSettings["SecretKey"];
    var isProduction = builder.Environment.IsProduction();

    if (string.IsNullOrWhiteSpace(jwtSecret) || (isProduction && jwtSecret.Contains("CHANGE_ME")))
        throw new InvalidOperationException(
            "FATAL: JWT SecretKey is not configured or uses the default value. " +
            "Set Jwt__SecretKey in environment variables.");

    var aiEncKey = builder.Configuration["Ai:EncryptionKey"];
    if (isProduction && (string.IsNullOrWhiteSpace(aiEncKey) || aiEncKey.Contains("CHANGE_ME")))
        Log.Warning("Ai:EncryptionKey uses the default value — set a strong key in production.");

    // ── Serilog ───────────────────────────────────────────────────────────────
    // Pre-create the final logger BEFORE builder.Build() to avoid any deferred
    // callback that deadlocks against .NET 10's hosting pipeline.
    // builder.Host.UseSerilog() with callbacks deadlocks (futex_do_wait, 17 threads).
    // Using builder.Logging.AddSerilog() with a pre-built logger has no callbacks.
    Console.WriteLine("DIAG1: building final Serilog logger");
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "OreoLeads")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
        .CreateLogger();
    Console.WriteLine("DIAG2: final logger built, calling AddSerilog");

    builder.Logging.ClearProviders();
    builder.Logging.AddSerilog(Log.Logger, dispose: true);
    Console.WriteLine("DIAG3: AddSerilog done, calling AddInfrastructure");

    // ── Infrastructure (EF Core, Redis, Identity, AI...) ─────────────────────
    builder.Services.AddInfrastructure(builder.Configuration);
    Console.WriteLine("DIAG4: AddInfrastructure done");

    // OpenTelemetry disabled: WithTracing/WithMetrics providers deadlock against
    // Serilog's ILoggerFactory replacement inside builder.Build() on .NET 10.
    // Re-enable once the SDK compatibility issue is resolved.
    // builder.Services.AddOreoLeadsObservability(builder.Configuration);

    // ── Controllers ───────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // ── Swagger / OpenAPI ─────────────────────────────────────────────────────
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "OreoLeads API",
            Version = "v1",
            Description = "CRM de prospection pour Oreo Studios"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Entrez votre token JWT : Bearer {token}",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer", doc),
                new List<string>()
            }
        });
    });

    // ── JWT Authentication ────────────────────────────────────────────────────
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

    builder.Services.AddAuthorization();

    // ── HSTS ──────────────────────────────────────────────────────────────────
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
        options.Preload = true;
    });

    // ── Rate Limiting ─────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(opts =>
    {
        var rateLimitSection = builder.Configuration.GetSection(RateLimitOptions.SectionName);
        var globalPermitLimit = rateLimitSection.GetValue<int>("PermitLimit", 100);
        var globalWindowSeconds = rateLimitSection.GetValue<int>("WindowSeconds", 60);
        var globalQueueLimit = rateLimitSection.GetValue<int>("QueueLimit", 10);

        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        opts.AddFixedWindowLimiter("global", limiter =>
        {
            limiter.PermitLimit = globalPermitLimit;
            limiter.Window = TimeSpan.FromSeconds(globalWindowSeconds);
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = globalQueueLimit;
        });

        opts.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            await context.HttpContext.Response.WriteAsync(
                "Too many requests. Please try again later.", ct);
        };

        // Auth endpoints: strict — 10 req/min per IP
        opts.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

        // Search: 30 req/min per user
        opts.AddPolicy("search", context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

        // AI generation: 20 req/min per user
        opts.AddPolicy("ai", context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

        // Website analysis: 10 req/min per user
        opts.AddPolicy("analyze", context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));
    });

    // ── CORS ──────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
                .WithOrigins(
                    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? ["http://localhost:5173"])
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    // ── Health Checks ─────────────────────────────────────────────────────────
    var redisHcString = builder.Configuration.GetConnectionString("Redis");
    var healthChecks = builder.Services.AddHealthChecks()
        .AddNpgSql(
            connectionString: builder.Configuration.GetConnectionString("DefaultConnection") ?? "",
            name: "postgresql",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
            tags: ["db", "sql"])
        .AddCheck<AutomationHealthCheck>("automation", tags: ["services"])
        .AddCheck<BackgroundServicesHealthCheck>("background-services", tags: ["services"]);

    // Only register the Redis health check when a non-empty connection string is available
    if (!string.IsNullOrWhiteSpace(redisHcString))
    {
        healthChecks.AddRedis(
            redisConnectionString: redisHcString,
            name: "redis",
            failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
            tags: ["cache"]);
    }

    builder.Services.AddTransient<AutomationHealthCheck>();
    builder.Services.AddTransient<BackgroundServicesHealthCheck>();

    // Prevent .NET 10 WebApplicationBuilder from eagerly resolving all singletons
    // (ValidateOnBuild=true in .NET 10 triggers IConnectionMultiplexer factory which
    // calls ConnectionMultiplexer.Connect() synchronously → deadlock via futex_do_wait)
    builder.Host.UseDefaultServiceProvider(options =>
    {
        options.ValidateScopes = false;
        options.ValidateOnBuild = false;
    });

    Console.WriteLine("DIAG5: calling builder.Build()");
    var app = builder.Build();
    Console.WriteLine("DIAG6: builder.Build() returned");

    // ── Security headers ─────────────────────────────────────────────────────
    app.UseMiddleware<SecurityHeadersMiddleware>();
    Console.WriteLine("DIAG7: middleware pipeline configured");

    // ── HSTS & HTTPS (production only) ───────────────────────────────────────
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    // ── TenantContext middleware — wire OrganizationId into EF query filters ──
    app.Use(async (context, next) =>
    {
        var tenantCtx = context.RequestServices.GetRequiredService<TenantContext>();
        var currentUser = context.RequestServices.GetRequiredService<ICurrentUserService>();
        if (currentUser.OrganizationId.HasValue)
            tenantCtx.SetOrganization(currentUser.OrganizationId.Value);
        await next();
    });

    app.UseMiddleware<CorrelationIdMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "OreoLeads API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseRateLimiter();
    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    Console.WriteLine("DIAG8: routes mapped");

    // ── Health check endpoints ────────────────────────────────────────────────
    app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds,
                    error = e.Value.Exception?.Message
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds,
                timestamp = DateTime.UtcNow
            });
            await context.Response.WriteAsync(result);
        }
    }).AllowAnonymous();

    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("db")
    }).AllowAnonymous();

    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // Always healthy if the process responds
    }).AllowAnonymous();

    // ── Startup tasks ─────────────────────────────────────────────────────────
    Console.WriteLine("DIAG9: starting startup tasks (migrations)");
    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILogger<Program>>();

        // Auto-migrate database on startup
        var db = sp.GetRequiredService<ApplicationDbContext>();
        Console.WriteLine("DIAG10: calling MigrateAsync");
        try
        {
            await db.Database.MigrateAsync();
            Console.WriteLine("DIAG11: MigrateAsync done");
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DIAG11-ERR: MigrateAsync failed: {ex.Message}");
            logger.LogError(ex, "Failed to apply database migrations.");
            if (!app.Environment.IsDevelopment()) throw;
        }

        // Seed Identity roles
        Console.WriteLine("DIAG12: seeding roles");
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Manager", "Sales" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed default AI prompt templates
        Console.WriteLine("DIAG13: seeding AI prompts");
        var aiConfig = sp.GetRequiredService<IAiConfigurationService>();
        await aiConfig.SeedDefaultPromptsAsync();
        Console.WriteLine("DIAG14: startup tasks complete");
    }

    Console.WriteLine("DIAG15: calling app.RunAsync()");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
