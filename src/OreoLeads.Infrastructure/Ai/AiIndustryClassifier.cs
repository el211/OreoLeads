using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Ai;

/// <summary>
/// Déduit le secteur d'activité d'un lot de prospects en un seul appel IA,
/// à partir du nom, de la description et du code NAF, puis l'enregistre.
/// </summary>
internal sealed class AiIndustryClassifier : IIndustryClassifier
{
    // Plafond pour maîtriser la taille du prompt / le coût par appel.
    private const int MaxLeadsPerCall = 100;

    private const string SystemPrompt = """
        Tu es un assistant qui détermine le secteur d'activité d'entreprises françaises.
        On te donne une liste d'entreprises (id, nom, description, code NAF).
        Pour CHAQUE entreprise, renvoie un secteur d'activité court et clair en français
        (2 à 4 mots : ex. "Restauration", "Coiffure", "Plomberie", "Notariat", "Boulangerie",
        "Garage automobile", "Institut de beauté").
        Réponds UNIQUEMENT avec un tableau JSON, sans texte autour :
        [{"id":"<id fourni>","secteur":"<secteur>"}]
        Reprends exactement les id fournis. N'invente aucun autre champ.
        """;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _db;
    private readonly IAiConfigurationService _aiConfig;
    private readonly IEnumerable<IAiProvider> _providers;
    private readonly ILogger<AiIndustryClassifier> _logger;

    public AiIndustryClassifier(
        ApplicationDbContext db,
        IAiConfigurationService aiConfig,
        IEnumerable<IAiProvider> providers,
        ILogger<AiIndustryClassifier> logger)
    {
        _db        = db;
        _aiConfig  = aiConfig;
        _providers = providers;
        _logger    = logger;
    }

    public async Task<int> AutofillAsync(IReadOnlyCollection<Guid> leadIds, CancellationToken ct = default)
    {
        if (leadIds.Count == 0) return 0;
        if (leadIds.Count > MaxLeadsPerCall)
            throw new InvalidOperationException(
                $"Trop de prospects sélectionnés ({leadIds.Count}). Maximum {MaxLeadsPerCall} à la fois.");

        var config = await _aiConfig.GetCurrentAsync()
                     ?? throw new InvalidOperationException("L'IA n'est pas configurée.");
        if (!config.IsEnabled)
            throw new InvalidOperationException("L'IA est désactivée. Activez-la dans les paramètres.");

        var provider = _providers.FirstOrDefault(p => p.ProviderType == config.ProviderType)
                       ?? throw new InvalidOperationException($"Fournisseur '{config.ProviderType}' non enregistré.");

        var leads = await _db.Leads.Where(l => leadIds.Contains(l.Id)).ToListAsync(ct);
        if (leads.Count == 0) return 0;

        var input = leads.Select(l => new
        {
            id = l.Id.ToString(),
            nom = l.CompanyName,
            description = l.Description,
            naf = l.NafCode,
        });
        var userPrompt = JsonSerializer.Serialize(input, JsonOpts);

        var completion = await provider.CompleteAsync(
            new AiCompletionRequest(SystemPrompt, userPrompt, MaxTokens: 1500, Temperature: 0.1f), ct);

        var results = ParseResults(completion.Content);
        if (results.Count == 0) return 0;

        var bySector = results
            .Where(r => !string.IsNullOrWhiteSpace(r.Id) && !string.IsNullOrWhiteSpace(r.Secteur))
            .ToDictionary(r => r.Id!, r => r.Secteur!.Trim(), StringComparer.OrdinalIgnoreCase);

        var updated = 0;
        foreach (var lead in leads)
        {
            if (bySector.TryGetValue(lead.Id.ToString(), out var secteur))
            {
                lead.Industry = secteur;
                lead.SetUpdatedAt();
                updated++;
            }
        }

        if (updated > 0)
            await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Autofill secteur IA : {Updated}/{Total} prospects mis à jour.", updated, leads.Count);
        return updated;
    }

    private List<ClassificationResult> ParseResults(string content)
    {
        var start = content.IndexOf('[');
        var end = content.LastIndexOf(']');
        if (start < 0 || end <= start)
        {
            _logger.LogWarning("Réponse IA sans tableau JSON exploitable : {Content}", content);
            return [];
        }

        var json = content[start..(end + 1)];
        try
        {
            return JsonSerializer.Deserialize<List<ClassificationResult>>(json, JsonOpts) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON IA invalide : {Json}", json);
            return [];
        }
    }

    private sealed class ClassificationResult
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("secteur")] public string? Secteur { get; set; }
    }
}
