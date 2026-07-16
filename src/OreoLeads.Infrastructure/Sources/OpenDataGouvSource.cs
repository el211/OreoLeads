using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Search.DTOs;

namespace OreoLeads.Infrastructure.Sources;

/// <summary>
/// Fournisseur de données basé sur l'API officielle du gouvernement français :
/// https://recherche-entreprises.api.gouv.fr
/// Aucune clé API requise — données publiques INSEE/SIRENE.
/// </summary>
public class OpenDataGouvSource : ILeadSource
{
    private const string BaseUrl = "https://recherche-entreprises.api.gouv.fr/search";
    private const int ApiPageSize = 25; // max autorisé par l'API

    private readonly HttpClient _http;
    private readonly ILogger<OpenDataGouvSource> _logger;

    public string ProviderName => "OpenDataGouv";

    public OpenDataGouvSource(HttpClient http, ILogger<OpenDataGouvSource> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<(List<CompanySearchResultDto> Results, int TotalFound)> SearchAsync(
        CompanySearchRequestDto request,
        int page = 1,
        CancellationToken ct = default)
    {
        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Keywords)) queryParts.Add($"q={Uri.EscapeDataString(request.Keywords)}");
        if (!string.IsNullOrWhiteSpace(request.NafCode)) queryParts.Add($"code_naf={Uri.EscapeDataString(request.NafCode)}");
        if (!string.IsNullOrWhiteSpace(request.Department)) queryParts.Add($"departement={Uri.EscapeDataString(request.Department)}");
        if (!string.IsNullOrWhiteSpace(request.Region)) queryParts.Add($"region={Uri.EscapeDataString(request.Region)}");
        if (!string.IsNullOrWhiteSpace(request.PostalCode)) queryParts.Add($"code_postal={Uri.EscapeDataString(request.PostalCode)}");
        if (request.ActiveOnly) queryParts.Add("est_active=true");
        queryParts.Add($"per_page={ApiPageSize}");
        queryParts.Add($"page={page}");

        var url = $"{BaseUrl}?{string.Join("&", queryParts)}";
        _logger.LogInformation("OpenDataGouv search: {Url}", url);

        GouvApiResponse? response;
        try
        {
            response = await _http.GetFromJsonAsync<GouvApiResponse>(url,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenDataGouv API error for URL: {Url}", url);
            return (new List<CompanySearchResultDto>(), 0);
        }

        if (response?.Results == null) return (new List<CompanySearchResultDto>(), 0);

        var results = response.Results
            .Select(MapToDto)
            .Where(r => !string.IsNullOrWhiteSpace(r.CompanyName))
            .ToList();

        // Filtrage ville côté client si fourni (l'API ne supporte pas ce filtre directement)
        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.ToLowerInvariant();
            results = results.Where(r =>
                r.City != null && r.City.ToLowerInvariant().Contains(city)).ToList();
        }

        // Filtrage secteur côté client si fourni
        if (!string.IsNullOrWhiteSpace(request.Industry))
        {
            var industry = request.Industry.ToLowerInvariant();
            results = results.Where(r =>
                r.Industry != null && r.Industry.ToLowerInvariant().Contains(industry)).ToList();
        }

        var maxResults = Math.Min(request.MaxResults, results.Count);
        return (results.Take(maxResults).ToList(), response.TotalResults);
    }

    private CompanySearchResultDto MapToDto(GouvCompany c) => new()
    {
        CompanyName = c.NomComplet ?? c.NomRaisonSociale ?? string.Empty,
        Siren = c.Siren,
        Siret = c.Siege?.Siret ?? c.Siren,
        Industry = c.LibelleActivitePrincipale,
        NafCode = c.ActivitePrincipale,
        Address = c.Siege?.AdresseLigne1,
        PostalCode = c.Siege?.CodePostal,
        City = c.Siege?.LibelleCommune,
        Department = c.Siege?.Departement,
        Region = c.Siege?.Region,
        Latitude = TryParseDouble(c.Siege?.Latitude),
        Longitude = TryParseDouble(c.Siege?.Longitude),
        EmployeeCount = MapEmployeeCount(c.Siege?.TrancheEffectifSalarie ?? c.TrancheEffectifSalarie),
        IsActive = c.EtatAdministratif == "A",
        Provider = ProviderName,
    };

    private static double? TryParseDouble(string? value)
        => double.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null;

    private static int? MapEmployeeCount(string? tranche) => tranche switch
    {
        "00" => 0,
        "01" => 1,
        "02" => 3,
        "03" => 6,
        "11" => 10,
        "12" => 20,
        "21" => 50,
        "22" => 100,
        "31" => 200,
        "32" => 250,
        "41" => 500,
        "42" => 1000,
        "51" => 2000,
        "52" => 5000,
        "53" => 10000,
        _ => null
    };

    // ── Modèles de désérialisation ──────────────────────────────────────────

    private sealed class GouvApiResponse
    {
        [JsonPropertyName("results")]
        public List<GouvCompany> Results { get; set; } = new();

        [JsonPropertyName("total_results")]
        public int TotalResults { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }
    }

    private sealed class GouvCompany
    {
        [JsonPropertyName("nom_complet")]
        public string? NomComplet { get; set; }

        [JsonPropertyName("nom_raison_sociale")]
        public string? NomRaisonSociale { get; set; }

        [JsonPropertyName("siren")]
        public string? Siren { get; set; }

        [JsonPropertyName("activite_principale")]
        public string? ActivitePrincipale { get; set; }

        [JsonPropertyName("libelle_activite_principale")]
        public string? LibelleActivitePrincipale { get; set; }

        [JsonPropertyName("etat_administratif")]
        public string? EtatAdministratif { get; set; }

        [JsonPropertyName("tranche_effectif_salarie")]
        public string? TrancheEffectifSalarie { get; set; }

        [JsonPropertyName("siege")]
        public GouvSiege? Siege { get; set; }
    }

    private sealed class GouvSiege
    {
        [JsonPropertyName("siret")]
        public string? Siret { get; set; }

        [JsonPropertyName("adresse_ligne_1")]
        public string? AdresseLigne1 { get; set; }

        [JsonPropertyName("code_postal")]
        public string? CodePostal { get; set; }

        [JsonPropertyName("libelle_commune")]
        public string? LibelleCommune { get; set; }

        [JsonPropertyName("departement")]
        public string? Departement { get; set; }

        [JsonPropertyName("region")]
        public string? Region { get; set; }

        [JsonPropertyName("latitude")]
        public string? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public string? Longitude { get; set; }

        [JsonPropertyName("tranche_effectif_salarie")]
        public string? TrancheEffectifSalarie { get; set; }
    }
}
