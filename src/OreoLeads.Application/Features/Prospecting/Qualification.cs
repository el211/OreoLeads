using OreoLeads.Domain.Entities;

namespace OreoLeads.Application.Features.Prospecting;

public record QualificationEvidence(string Dimension, int Points, string Observation, string? SourceUrl, DateTime ObservedAt, double Confidence);

public static class Qualification
{
    public static List<QualificationEvidence> ApplyWeights(List<QualificationEvidence> evidence, IReadOnlyDictionary<string, int> weights)
    {
        if (weights.Count == 0) return evidence;
        if (weights.Values.Any(v => v < 0 || v > 100) || weights.Values.Sum() != 100)
            throw new InvalidOperationException("Qualification weights must be nonnegative and sum to 100.");
        return evidence.Select(e => e with { Points = weights.GetValueOrDefault(e.Dimension, 0) }).ToList();
    }
    public static List<QualificationEvidence> Evaluate(Lead lead, OreoLeads.Domain.Entities.WebsiteAnalysis? analysis, LeadEnrichment? enrichment)
    {
        var evidence = new List<QualificationEvidence>();
        var registry = lead.Siren == null ? null : $"https://annuaire-entreprises.data.gouv.fr/entreprise/{lead.Siren}";
        if (registry != null) evidence.Add(new("Business", 20, "Company matched in French registry", registry, lead.CreatedAt, 1));
        if (!string.IsNullOrWhiteSpace(lead.City)) evidence.Add(new("Geography", 15, $"Registry location: {lead.City}", registry, lead.CreatedAt, 1));
        if (lead.EmployeeCount is >= 0 and <= 50) evidence.Add(new("Size", 10, $"Registry employee-band lower bound: {lead.EmployeeCount}", registry, lead.CreatedAt, .8));
        if (enrichment?.DiscoveredEmail != null && enrichment.EmailSourceUrl != null)
            evidence.Add(new("Contactability", 20, "Public business email observed; marketing permission requires review", enrichment.EmailSourceUrl, enrichment.UpdatedAt ?? enrichment.CreatedAt, enrichment.EmailConfidence ?? 0));
        // Failed requests and default boolean values are never evidence of website defects.
        if (analysis is { AnalysisError: null, HttpStatus: >= 200 and < 300, PageTitle: not null })
        {
            evidence.Add(new("Confidence", 10, "Public HTML successfully inspected", analysis.Url, analysis.LastAnalysis, 1));
            if (!analysis.HasViewport) evidence.Add(new("Website", 15, "Viewport meta tag not observed in inspected HTML; mobile behaviour needs review", analysis.Url, analysis.LastAnalysis, .9));
            if (analysis.ResponseTimeMs > 3000) evidence.Add(new("Performance", 5, $"Fetch took {analysis.ResponseTimeMs} ms including policy checks; not a Core Web Vitals measurement", analysis.Url, analysis.LastAnalysis, 1));
            if (!analysis.UsesHttps) evidence.Add(new("Transport", 5, "Inspected URL uses HTTP; HTTPS availability is unknown", analysis.Url, analysis.LastAnalysis, 1));
        }
        return evidence;
    }
}
