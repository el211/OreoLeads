using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Infrastructure.Extensions;
using OreoLeads.Infrastructure.Identity;
using OreoLeads.Infrastructure.Persistence;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
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
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/oreo-.log", rollingInterval: RollingInterval.Day));

    // ── Infrastructure (EF Core, Redis, Identity, AI...) ─────────────────────
    builder.Services.AddInfrastructure(builder.Configuration);

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

    // ── Rate Limiting ─────────────────────────────────────────────────────────
    builder.Services.AddRateLimiter(opts =>
    {
        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

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
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
        .AddCheck<OreoLeads.Api.HealthChecks.DatabaseHealthCheck>("database");

    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── TenantContext middleware — wire OrganizationId into EF query filters ──
    app.Use(async (context, next) =>
    {
        var tenantCtx = context.RequestServices.GetRequiredService<TenantContext>();
        var currentUser = context.RequestServices.GetRequiredService<ICurrentUserService>();
        if (currentUser.OrganizationId.HasValue)
            tenantCtx.SetOrganization(currentUser.OrganizationId.Value);
        await next();
    });

    app.UseMiddleware<OreoLeads.Api.Middleware.CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

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

    // ── Health check endpoints ────────────────────────────────────────────────
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Name == "self",
    }).AllowAnonymous();

    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Name == "database",
    }).AllowAnonymous();

    app.MapHealthChecks("/health").AllowAnonymous();

    // ── Startup tasks ─────────────────────────────────────────────────────────
    using (var scope = app.Services.CreateScope())
    {
        var sp = scope.ServiceProvider;

        // Seed Identity roles
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Manager", "Sales" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed default AI prompt templates
        var aiConfig = sp.GetRequiredService<IAiConfigurationService>();
        await aiConfig.SeedDefaultPromptsAsync();
    }

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
