using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Prospecting;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Prospecting;

public sealed class CampaignRecommendationService(ApplicationDbContext db, ILeadQualificationService ai)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public async Task<LeadRecommendation> GenerateAsync(Guid id, Guid organizationId, CancellationToken ct)
    {
        var prospect = await db.CampaignProspects.AsNoTracking().SingleAsync(p => p.Id == id && p.OrganizationId == organizationId, ct);
        if (prospect.RecommendationJson != null) return JsonSerializer.Deserialize<LeadRecommendation>(prospect.RecommendationJson, Json)!;
        if (prospect.QualifiedAt == null) throw new InvalidOperationException("Qualification has not completed.");
        var now = DateTime.UtcNow;
        var lease = Guid.NewGuid();
        var claimed = await db.CampaignProspects.Where(p => p.Id == id && p.OrganizationId == organizationId && p.RecommendationJson == null &&
            p.AiAttempts < 3 && (p.AiLeaseUntil == null || p.AiLeaseUntil < now) && (p.NextAiAttemptAt == null || p.NextAiAttemptAt <= now))
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.AiLeaseToken, lease).SetProperty(p => p.AiLeaseUntil, now.AddMinutes(3))
                .SetProperty(p => p.AiAttempts, p => p.AiAttempts + 1), ct);
        if (claimed == 0) throw new InvalidOperationException("Recommendation is running, waiting for retry, or exhausted its three attempts.");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(90));
            var evidence = JsonSerializer.Deserialize<List<QualificationEvidence>>(prospect.EvidenceJson, Json) ?? [];
            var result = await ai.RecommendAsync(evidence, timeout.Token);
            var json = JsonSerializer.Serialize(result, Json);
            var completed = await db.CampaignProspects.Where(p => p.Id == id && p.AiLeaseToken == lease)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.RecommendationJson, json).SetProperty(p => p.AiError, (string?)null)
                    .SetProperty(p => p.AiLeaseUntil, (DateTime?)null).SetProperty(p => p.AiLeaseToken, (Guid?)null), ct);
            if (completed == 0) throw new InvalidOperationException("Recommendation lease was lost.");
            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            var error = ex.Message[..Math.Min(ex.Message.Length, 1000)];
            var retryAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, prospect.AiAttempts + 1));
            await db.CampaignProspects.Where(p => p.Id == id && p.AiLeaseToken == lease)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.AiError, error).SetProperty(p => p.NextAiAttemptAt, retryAt)
                    .SetProperty(p => p.AiLeaseUntil, (DateTime?)null).SetProperty(p => p.AiLeaseToken, (Guid?)null), ct);
            throw;
        }
    }
}
