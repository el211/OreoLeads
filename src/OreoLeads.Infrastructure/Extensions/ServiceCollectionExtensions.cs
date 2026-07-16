using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OreoLeads.Application.Common.Interfaces;
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

        // Services
        services.AddScoped<ICsvImportService, CsvImportService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISearchService, SearchService>();

        // Search — fournisseurs de données (ILeadSource)
        services.AddHttpClient<OpenDataGouvSource>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "OreoLeads/1.0");
        });
        services.AddScoped<ILeadSource, OpenDataGouvSource>();

        // Search repository
        services.AddScoped<ISearchRepository, SearchRepository>();

        // Ajouter aussi un repository pour les notes (géré directement via context)
        services.AddScoped<LeadNoteRepository>();

        // FluentValidation — charge les validators depuis l'assembly Application
        services.AddValidatorsFromAssembly(typeof(IApplicationDbContext).Assembly);

        return services;
    }
}
