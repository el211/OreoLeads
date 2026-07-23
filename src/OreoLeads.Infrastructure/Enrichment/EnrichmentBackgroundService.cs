using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Enrichment;

/// <summary>
/// Traite la file d'enrichissement en arrière-plan. Chaque job s'exécute dans son
/// propre scope DI (le DbContext scoped n'est pas thread-safe) et la concurrence
/// est bornée par un SemaphoreSlim.
/// </summary>
internal sealed class EnrichmentBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EnrichmentSettings _settings;
    private readonly ILogger<EnrichmentBackgroundService> _logger;

    public EnrichmentBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<EnrichmentSettings> settings,
        ILogger<EnrichmentBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _settings     = settings.Value;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EnrichmentBackgroundService started.");
        var tick = TimeSpan.FromSeconds(Math.Max(1, _settings.TickSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in EnrichmentBackgroundService tick.");
            }

            await Task.Delay(tick, stoppingToken);
        }

        _logger.LogInformation("EnrichmentBackgroundService stopped.");
    }

    private async Task ProcessTickAsync(CancellationToken ct)
    {
        List<Guid> jobIds;
        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var queue = scope.ServiceProvider.GetRequiredService<IEnrichmentQueueService>();
            var pending = await queue.GetPendingAsync(_settings.MaxJobsPerTick, ct);
            jobIds = pending.Select(j => j.Id).ToList();
        }

        if (jobIds.Count == 0) return;

        _logger.LogDebug("Traitement de {Count} job(s) d'enrichissement.", jobIds.Count);

        using var throttle = new SemaphoreSlim(Math.Max(1, _settings.MaxConcurrentJobs));
        var tasks = jobIds.Select(async id =>
        {
            await throttle.WaitAsync(ct);
            try
            {
                await ProcessJobAsync(id, ct);
            }
            finally
            {
                throttle.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task ProcessJobAsync(Guid jobId, CancellationToken ct)
    {
        // Un scope DI par job — le DbContext scoped n'est pas partageable entre threads.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var queue = scope.ServiceProvider.GetRequiredService<IEnrichmentQueueService>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ICompanyEnrichmentService>();

        using var logScope = _logger.BeginScope(new Dictionary<string, object> { ["EnrichmentId"] = jobId });

        try
        {
            await queue.MarkRunningAsync(jobId, ct);
            var status = await orchestrator.RunAsync(jobId, ct);

            if (status == EnrichmentStatus.NeedsReview)
                await queue.MarkNeedsReviewAsync(jobId, ct);
            else
                await queue.MarkCompletedAsync(jobId, ct);

            _logger.LogInformation("Job d'enrichissement {JobId} terminé : {Status}", jobId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job d'enrichissement {JobId} en échec.", jobId);
            try
            {
                await queue.MarkFailedAsync(jobId, ex.Message, canRetry: true, ct);
            }
            catch (Exception markEx)
            {
                _logger.LogError(markEx, "Impossible de marquer le job {JobId} en échec.", jobId);
            }
        }
    }
}
