using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Infrastructure.Ai;
using OreoLeads.Infrastructure.Ai.Providers;
using OreoLeads.Infrastructure.Airtable;
using OreoLeads.Infrastructure.Analytics;
using OreoLeads.Infrastructure.Analytics.BackgroundServices;
using OreoLeads.Infrastructure.Automation;
using OreoLeads.Infrastructure.Automation.Actions;
using OreoLeads.Infrastructure.Automation.BackgroundServices;
using OreoLeads.Infrastructure.Brevo;
using OreoLeads.Infrastructure.Enrichment;
using OreoLeads.Infrastructure.Fetching;
using OreoLeads.Infrastructure.Identity;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Security;
using OreoLeads.Infrastructure.Persistence.Repositories;
using OreoLeads.Infrastructure.Services;
using OreoLeads.Infrastructure.Smtp;
using OreoLeads.Infrastructure.Sources;
using StackExchange.Redis;

namespace OreoLeads.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Shared encryption (AES-256-GCM) — singleton, key is stable for app lifetime ──
        services.AddSingleton<IEncryptionService, EncryptionService>();

        // TenantContext — must be scoped so EF query filters get per-request values
        services.AddScoped<TenantContext>();

        // PostgreSQL via EF Core (inject TenantContext)
        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // ASP.NET Core Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Redis — registered as a lazy factory so the synchronous Connect() call
        // never blocks DI registration (which runs before builder.Build()).
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var redisConfig = ConfigurationOptions.Parse(redisConnectionString);
                redisConfig.AbortOnConnectFail = false;
                redisConfig.ConnectTimeout = 5000;
                return ConnectionMultiplexer.Connect(redisConfig);
            });
        }

        // HTTP context accessor (for CurrentUserService, AuditLogger)
        services.AddHttpContextAccessor();

        // Repositories
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<ILeadActivityRepository, LeadActivityRepository>();
        services.AddScoped<IFollowUpRepository, FollowUpRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ISearchRepository, SearchRepository>();
        services.AddScoped<IEmailDraftRepository, EmailDraftRepository>();
        services.AddScoped<IPromptTemplateRepository, PromptTemplateRepository>();

        // Services
        services.AddScoped<ICsvImportService, CsvImportService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IWebsiteAnalyzerService, WebsiteAnalyzerService>();

        // Also expose LeadNoteRepository directly
        services.AddScoped<LeadNoteRepository>();

        // Search — fournisseurs de données (ILeadSource)
        services.AddHttpClient<OpenDataGouvSource>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "OreoLeads/1.0");
        });
        services.AddScoped<ILeadSource, OpenDataGouvSource>();

        // ── Enrichissement (découverte site/e-mail + Playwright) ────────────────
        services.Configure<BraveSearchSettings>(configuration.GetSection(BraveSearchSettings.Section));
        services.Configure<EnrichmentSettings>(configuration.GetSection(EnrichmentSettings.Section));
        services.Configure<PlaywrightSettings>(configuration.GetSection(PlaywrightSettings.Section));

        services.AddHttpClient(nameof(BraveSearchClient), c => c.Timeout = TimeSpan.FromSeconds(20));
        services.AddSingleton<IBraveSearchClient, BraveSearchClient>();

        // Fetchers : Playwright + HTTP en singletons, exposés via CompositePageFetcher
        services.AddSingleton<HttpPageFetcher>();
        services.AddSingleton<PlaywrightPageFetcher>();
        services.AddSingleton<IPageFetcher, CompositePageFetcher>();

        services.AddScoped<IWebsiteDiscoveryService, WebsiteDiscoveryService>();
        services.AddScoped<IEmailDiscoveryService, EmailDiscoveryService>();
        services.AddScoped<ICompanyEnrichmentService, CompanyEnrichmentService>();
        services.AddScoped<IEnrichmentQueueService, EnrichmentQueueService>();
        services.AddScoped<IEnrichmentService, EnrichmentService>();
        services.AddHostedService<EnrichmentBackgroundService>();

        // ── AI ────────────────────────────────────────────────────────────────
        services.AddScoped<IAiConfigurationService, AiConfigurationService>();
        services.AddScoped<IPromptBuilder, PromptBuilder>();
        services.AddScoped<IEmailGeneratorService, EmailGeneratorService>();
        services.AddScoped<ISearchQueryParser, AiSearchQueryParser>();
        services.AddScoped<IIndustryClassifier, AiIndustryClassifier>();

        // AI Providers — each has its own typed HttpClient + is exposed as IAiProvider
        services.AddHttpClient<ClaudeAiProvider>(c => c.Timeout = TimeSpan.FromSeconds(60));
        services.AddScoped<ClaudeAiProvider>();

        services.AddHttpClient<OpenAiProvider>(c => c.Timeout = TimeSpan.FromSeconds(60));
        services.AddScoped<OpenAiProvider>();

        services.AddHttpClient<OllamaProvider>(c => c.Timeout = TimeSpan.FromSeconds(120));
        services.AddScoped<OllamaProvider>();

        services.AddHttpClient<GenericOpenAiProvider>(c => c.Timeout = TimeSpan.FromSeconds(60));
        services.AddScoped<GenericOpenAiProvider>();

        // Expose all providers as IEnumerable<IAiProvider>
        services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<ClaudeAiProvider>());
        services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<OpenAiProvider>());
        services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<OllamaProvider>());
        services.AddScoped<IAiProvider>(sp => sp.GetRequiredService<GenericOpenAiProvider>());

        // ── Identity / Auth ───────────────────────────────────────────────────
        services.AddScoped<JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        // ── SMTP (own mailbox — OVH, Namecheap, Gmail…) ──────────────────────
        services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.Section));
        services.AddSingleton<SmtpEmailSender>();

        // ── Brevo ─────────────────────────────────────────────────────────────
        services.AddHttpClient<BrevoService>(c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IBrevoService, BrevoService>();
        services.AddScoped<IBrevoConfigurationService, BrevoConfigurationService>();
        services.AddScoped<IEmailQueueService, EmailQueueService>();
        services.AddScoped<IEmailStatsService, EmailStatsService>();
        services.AddHostedService<EmailSendBackgroundService>();

        // ── Airtable ──────────────────────────────────────────────────────────
        services.AddHttpClient<AirtableService>(c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IAirtableService, AirtableService>();
        services.AddScoped<IAirtableConfigurationService, AirtableConfigurationService>();
        services.AddScoped<IAirtableSyncService, AirtableSyncService>();
        services.AddScoped<IAirtableWebhookService, AirtableWebhookService>();
        services.AddHostedService<AirtableSyncBackgroundService>();

        // ── Automation Engine ──────────────────────────────────────────────────
        services.AddSingleton<IAutomationQueue, AutomationQueueService>();
        services.AddScoped<IAutomationEngine, AutomationEngine>();
        services.AddScoped<IAutomationScheduler, AutomationSchedulerService>();
        services.AddScoped<IAutomationExecutor, AutomationExecutorService>();
        services.AddScoped<IAutomationValidator, AutomationValidatorService>();
        services.AddScoped<IAutomationWorkflowService, AutomationWorkflowService>();

        // Action handlers
        services.AddScoped<SendEmailActionHandler>();
        services.AddScoped<ChangeStatusActionHandler>();
        services.AddScoped<AddTagActionHandler>();
        services.AddScoped<RemoveTagActionHandler>();
        services.AddScoped<CreateFollowUpActionHandler>();
        services.AddScoped<CreateNoteActionHandler>();
        services.AddScoped<WaitActionHandler>();
        services.AddScoped<SetVariableActionHandler>();
        services.AddScoped<ExecuteWorkflowActionHandler>();
        services.AddHttpClient<HttpRequestActionHandler>();
        services.AddScoped<HttpRequestActionHandler>();

        // Background services
        services.AddHostedService<AutomationSchedulerBackgroundService>();
        services.AddHostedService<AutomationQueueWorkerBackgroundService>();
        services.AddHostedService<AutomationRetryWorkerBackgroundService>();
        services.AddHostedService<AutomationCleanupWorkerBackgroundService>();
        services.AddHostedService<AutomationTimeoutWorkerBackgroundService>();

        // ── Analytics ──────────────────────────────────────────────────────────
        services.AddMemoryCache();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IForecastService, ForecastService>();
        services.AddScoped<IWidgetService, WidgetService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddHostedService<ScheduledReportBackgroundService>();

        // FluentValidation — charge les validators depuis l'assembly Application
        services.AddValidatorsFromAssembly(typeof(IApplicationDbContext).Assembly);

        return services;
    }
}
