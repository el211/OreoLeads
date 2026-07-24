using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Search.DTOs;

namespace OreoLeads.Infrastructure.Ai;

/// <summary>
/// Convertit une phrase (« coiffeurs à Strasbourg sans site web ») en filtres de
/// recherche, via le fournisseur IA configuré. L'IA ne produit que des filtres :
/// les entreprises réelles sont ensuite ramenées par l'API officielle.
/// </summary>
internal sealed class AiSearchQueryParser : ISearchQueryParser
{
    private const string SystemPrompt = """
        Tu es un assistant qui convertit une demande en français en filtres de recherche
        d'entreprises françaises (base SIRENE). Tu NE dois PAS inventer d'entreprises.
        Réponds UNIQUEMENT avec un objet JSON valide, sans texte autour, avec ces champs :
        {
          "keywords": string|null,      // activité ou type d'entreprise en toutes lettres (ex: "coiffeur", "restaurant", "plombier")
          "nafCode": string|null,       // code NAF seulement si tu es certain, sinon null
          "city": string|null,          // ville si précisée
          "postalCode": string|null,    // code postal si précisé
          "department": string|null,    // numéro de département si précisé (ex: "67")
          "region": string|null,        // nom de région si précisé
          "wholeFrance": boolean,       // true si la demande couvre toute la France / aucune localisation
          "onlyIndividualEntrepreneurs": boolean, // true si "auto-entrepreneur", "artisan", "indépendant"
          "wantsWebsite": boolean|null, // "avec site"=true, "sans site"=false, non précisé=null
          "wantsEmail": boolean|null,   // "avec email"=true, "sans email"=false, non précisé=null
          "interpretation": string      // courte reformulation en français de ce que tu as compris
        }
        Privilégie "keywords" (recherche plein-texte) plutôt que "nafCode".
        Si une localisation précise est donnée, wholeFrance=false.
        """;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly IAiConfigurationService _aiConfig;
    private readonly IEnumerable<IAiProvider> _providers;
    private readonly ILogger<AiSearchQueryParser> _logger;

    public AiSearchQueryParser(
        IAiConfigurationService aiConfig,
        IEnumerable<IAiProvider> providers,
        ILogger<AiSearchQueryParser> logger)
    {
        _aiConfig  = aiConfig;
        _providers = providers;
        _logger    = logger;
    }

    public async Task<AiSearchParseResultDto> ParseAsync(string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new InvalidOperationException("La requête est vide.");

        var config = await _aiConfig.GetCurrentAsync()
                     ?? throw new InvalidOperationException("L'IA n'est pas configurée.");
        if (!config.IsEnabled)
            throw new InvalidOperationException("L'IA est désactivée. Activez-la dans les paramètres.");

        var provider = _providers.FirstOrDefault(p => p.ProviderType == config.ProviderType)
                       ?? throw new InvalidOperationException($"Fournisseur '{config.ProviderType}' non enregistré.");

        var completion = await provider.CompleteAsync(
            new AiCompletionRequest(SystemPrompt, prompt, MaxTokens: 400, Temperature: 0.1f), ct);

        var parsed = ParseJson(completion.Content);
        return MapResult(parsed);
    }

    private ParsedFilters ParseJson(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            _logger.LogWarning("Réponse IA sans JSON exploitable : {Content}", content);
            throw new InvalidOperationException("L'IA n'a pas renvoyé de filtres exploitables.");
        }

        var json = content[start..(end + 1)];
        try
        {
            return JsonSerializer.Deserialize<ParsedFilters>(json, JsonOpts) ?? new ParsedFilters();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON IA invalide : {Json}", json);
            throw new InvalidOperationException("L'IA n'a pas renvoyé de filtres exploitables.");
        }
    }

    private static AiSearchParseResultDto MapResult(ParsedFilters p)
    {
        var whole = p.WholeFrance;
        var request = new CompanySearchRequestDto
        {
            Keywords   = Clean(p.Keywords),
            NafCode    = Clean(p.NafCode),
            City       = whole ? null : Clean(p.City),
            PostalCode = whole ? null : Clean(p.PostalCode),
            Department = whole ? null : Clean(p.Department),
            Region     = whole ? null : Clean(p.Region),
            OnlyIndividualEntrepreneurs = p.OnlyIndividualEntrepreneurs,
            ActiveOnly = true,
            MaxResults = 50,
        };

        return new AiSearchParseResultDto
        {
            Request        = request,
            WantsWebsite   = p.WantsWebsite,
            WantsEmail     = p.WantsEmail,
            Interpretation = Clean(p.Interpretation),
        };
    }

    private static string? Clean(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private sealed class ParsedFilters
    {
        [JsonPropertyName("keywords")] public string? Keywords { get; set; }
        [JsonPropertyName("nafCode")] public string? NafCode { get; set; }
        [JsonPropertyName("city")] public string? City { get; set; }
        [JsonPropertyName("postalCode")] public string? PostalCode { get; set; }
        [JsonPropertyName("department")] public string? Department { get; set; }
        [JsonPropertyName("region")] public string? Region { get; set; }
        [JsonPropertyName("wholeFrance")] public bool WholeFrance { get; set; }
        [JsonPropertyName("onlyIndividualEntrepreneurs")] public bool OnlyIndividualEntrepreneurs { get; set; }
        [JsonPropertyName("wantsWebsite")] public bool? WantsWebsite { get; set; }
        [JsonPropertyName("wantsEmail")] public bool? WantsEmail { get; set; }
        [JsonPropertyName("interpretation")] public string? Interpretation { get; set; }
    }
}
