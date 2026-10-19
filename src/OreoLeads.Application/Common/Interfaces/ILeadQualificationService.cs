using OreoLeads.Application.Features.Prospecting;

namespace OreoLeads.Application.Common.Interfaces;

public record LeadRecommendation(string Service, int[] EvidenceIndexes, string Provider, int Tokens);

public interface ILeadQualificationService
{
    Task<LeadRecommendation> RecommendAsync(IReadOnlyList<QualificationEvidence> evidence, CancellationToken ct);
}
