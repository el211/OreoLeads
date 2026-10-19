using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Prospecting;

public sealed class CampaignRecommendationWorker(IServiceScopeFactory scopes, ILogger<CampaignRecommendationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;
                var candidate = await (from p in db.CampaignProspects
                    join c in db.ProspectingCampaigns on p.CampaignId equals c.Id
                    where c.Status == "Active" && c.AutomaticAiRecommendation && p.State == "Review" && p.QualifiedAt != null && p.RecommendationJson == null &&
                        p.AiAttempts < 3 && (p.AiLeaseUntil == null || p.AiLeaseUntil < now) && (p.NextAiAttemptAt == null || p.NextAiAttemptAt <= now)
                    orderby p.CreatedAt
                    select new { p.Id, p.OrganizationId }).FirstOrDefaultAsync(stoppingToken);
                if (candidate != null)
                {
                    scope.ServiceProvider.GetRequiredService<TenantContext>().SetOrganization(candidate.OrganizationId);
                    await scope.ServiceProvider.GetRequiredService<CampaignRecommendationService>().GenerateAsync(candidate.Id, candidate.OrganizationId, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning(ex, "Campaign recommendation deferred"); }
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
