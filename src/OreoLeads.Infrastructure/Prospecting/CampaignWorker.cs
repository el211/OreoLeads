using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Prospecting;
using OreoLeads.Application.Features.Search.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Prospecting;

public sealed class CampaignWorker(IServiceScopeFactory scopes, ILogger<CampaignWorker> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ids = await db.ProspectingCampaigns.Where(c => c.Status == "Active" && c.NextRunAt <= DateTime.UtcNow)
                    .OrderBy(c => c.NextRunAt).Select(c => c.Id).Take(10).ToListAsync(stoppingToken);
                foreach (var id in ids) await ProcessAsync(id, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Campaign scheduler tick failed"); }
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    internal async Task ProcessAsync(Guid id, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenant = scope.ServiceProvider.GetRequiredService<TenantContext>();
        // Transaction advisory lock serializes campaign imports across replicas. A crash releases it,
        // and rolls back imports, cursor and quota together: no stale Running state to recover.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(713980124)", ct);
        var campaign = await db.ProspectingCampaigns.SingleAsync(c => c.Id == id, ct);
        if (campaign.Status != "Active" || campaign.NextRunAt > DateTime.UtcNow) return;
        tenant.SetOrganization(campaign.OrganizationId);
        try
        {
            await QualifyAsync(db, campaign, ct);
            if (campaign.QuotaDate != DateTime.UtcNow.Date)
            {
                campaign.QuotaDate = DateTime.UtcNow.Date;
                campaign.DiscoveredToday = campaign.PagesToday = 0;
            }
            var pending = await db.CampaignProspects.CountAsync(p => p.CampaignId == id && p.State == "Enriching", ct);
            if (campaign.NextDiscoveryAt > DateTime.UtcNow || pending >= campaign.MaxPendingEnrichments)
            {
                // Qualification keeps running even while discovery is waiting or backpressured.
            }
            else if (campaign.DiscoveredToday >= campaign.DailyLimit || campaign.PagesToday >= campaign.DailyPageLimit)
            {
                campaign.NextDiscoveryAt = DateTime.UtcNow.Date.AddDays(1);
            }
            else
            {
                var criteria = JsonSerializer.Deserialize<CompanySearchRequestDto>(campaign.CriteriaJson)!;
                var source = scope.ServiceProvider.GetServices<ILeadSource>().First();
                var (companies, _, rawCount) = await source.SearchAsync(criteria, campaign.NextPage, ct);
                var available = Math.Min(campaign.DailyLimit - campaign.DiscoveredToday, campaign.MaxPendingEnrichments - pending);
                if (available <= 0)
                {
                    campaign.NextDiscoveryAt = DateTime.UtcNow.Date.AddDays(1);
                }
                else
                {
                    var result = await scope.ServiceProvider.GetRequiredService<ISearchService>().ImportAsync(new()
                    { Companies = companies.Skip(campaign.PageOffset).Take(available).Where(c => !c.IsNonDiffusible).ToList() }, ct);
                    foreach (var leadId in result.NewLeadIds)
                    {
                        db.CampaignProspects.Add(new() { CampaignId = id, LeadId = leadId, OrganizationId = campaign.OrganizationId });
                        await scope.ServiceProvider.GetRequiredService<IEnrichmentQueueService>().QueueAsync(leadId, campaign.OrganizationId, false, ct);
                    }
                    db.CampaignExecutions.Add(new()
                    {
                        CampaignId = id, OrganizationId = campaign.OrganizationId, Page = campaign.NextPage,
                        Discovered = result.NewLeads, Duplicates = result.Duplicates + result.UpdatedLeads
                    });
                    campaign.PagesToday++;
                    campaign.DiscoveredToday += result.NewLeads;
                    campaign.PageOffset += Math.Min(available, Math.Max(0, companies.Count - campaign.PageOffset));
                    campaign.NextDiscoveryAt = DateTime.UtcNow.AddMinutes(campaign.DiscoveryIntervalMinutes);
                    if (campaign.PageOffset >= companies.Count)
                    {
                        campaign.PageOffset = 0;
                        campaign.NextPage++;
                        if (rawCount < source.PageSize) { campaign.NextPage = 1; campaign.NextDiscoveryAt = DateTime.UtcNow.AddDays(1); }
                    }
                }
            }
            campaign.Failures = 0;
            campaign.NextRunAt = DateTime.UtcNow.AddMinutes(1);
            campaign.LastError = null;
            campaign.SetUpdatedAt();
            var cutoff = DateTime.UtcNow.AddDays(-campaign.RetentionDays);
            await db.CampaignExecutions.Where(e => e.CampaignId == id && e.CreatedAt < cutoff).ExecuteDeleteAsync(ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            campaign = await db.ProspectingCampaigns.SingleAsync(c => c.Id == id, ct);
            campaign.Failures++;
            campaign.LastError = ex.Message[..Math.Min(ex.Message.Length, 1000)];
            campaign.NextRunAt = DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, Math.Min(campaign.Failures, 6))));
            if (campaign.Failures >= 8) campaign.Status = "Paused";
            db.CampaignExecutions.Add(new() { CampaignId = id, OrganizationId = campaign.OrganizationId, Page = campaign.NextPage, Outcome = "Failed", Error = campaign.LastError });
            await db.SaveChangesAsync(ct);
            logger.LogWarning(ex, "Campaign {CampaignId} deferred", id);
        }
    }

    private async Task QualifyAsync(ApplicationDbContext db, ProspectingCampaign campaign, CancellationToken ct)
    {
        var prospects = await db.CampaignProspects.Where(p => p.CampaignId == campaign.Id && p.State == "Enriching").Take(100).ToListAsync(ct);
        foreach (var prospect in prospects)
        {
            var enrichment = await db.LeadEnrichments.Where(e => e.LeadId == prospect.LeadId).OrderByDescending(e => e.CreatedAt).FirstOrDefaultAsync(ct);
            if (enrichment == null || enrichment.Status is EnrichmentStatus.Pending or EnrichmentStatus.Running) continue;
            var lead = await db.Leads.SingleAsync(l => l.Id == prospect.LeadId, ct);
            var analysis = await db.WebsiteAnalyses.Where(a => a.LeadId == lead.Id).OrderByDescending(a => a.CreatedAt).FirstOrDefaultAsync(ct);
            var weights = configuration.GetSection("Prospecting:Weights").Get<Dictionary<string, int>>() ?? [];
            var evidence = Qualification.ApplyWeights(Qualification.Evaluate(lead, analysis, enrichment), weights);
            prospect.EvidenceJson = JsonSerializer.Serialize(evidence, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            prospect.Score = Math.Clamp(evidence.Sum(e => e.Points), 0, 100);
            prospect.State = enrichment.Status == EnrichmentStatus.Failed ? "EnrichmentFailed" : prospect.Score >= campaign.MinimumScore ? "Review" : "BelowThreshold";
            prospect.QualifiedAt = DateTime.UtcNow;
            lead.Score = prospect.Score;
        }
    }
}
