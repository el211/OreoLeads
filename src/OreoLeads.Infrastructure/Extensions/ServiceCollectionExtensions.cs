using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Infrastructure.Ai;
using OreoLeads.Infrastructure.Ai.Providers;
using OreoLeads.Infrastructure.Brevo;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Persistence.Repositories;
using OreoLeads.Infrastructure.Services;
using OreoLeads.Infrastructure.Sources;
using StackExchange.Redis;

namespace OreoLeads.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL via EF Core
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Redis (abortConnect=false = ne crash pas si Redis est indisponible)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            var redisConfig = ConfigurationOptions.Parse(redisConnectionString);
            redisConfig.AbortOnConnectFail = false;
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConfig));
        }

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

        // ── AI ────────────────────────────────────────────────────────────────
        services.AddScoped<IAiConfigurationService, AiConfigurationService>();
        services.AddScoped<IPromptBuilder, PromptBuilder>();
        services.AddScoped<IEmailGeneratorService, EmailGeneratorService>();

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

        // ── Brevo ─────────────────────────────────────────────────────────────
        services.AddHttpClient<BrevoService>(c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IBrevoService, BrevoService>();
        services.AddScoped<IBrevoConfigurationService, BrevoConfigurationService>();
        services.AddScoped<IEmailQueueService, EmailQueueService>();
        services.AddScoped<IEmailStatsService, EmailStatsService>();
        services.AddHostedService<EmailSendBackgroundService>();

        // FluentValidation — charge les validators depuis l'assembly Application
        services.AddValidatorsFromAssembly(typeof(IApplicationDbContext).Assembly);

        return services;
    }
}
