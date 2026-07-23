using System.Globalization;
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
    internal const int ApiPageSize = 25; // max autorisé par l'API

    private const string NatureJuridiqueEntrepreneurIndividuel = "1000";

    private readonly HttpClient _http;
    private readonly ILogger<OpenDataGouvSource> _logger;

    public string ProviderName => "OpenDataGouv";
    public int PageSize => ApiPageSize;

    public OpenDataGouvSource(HttpClient http, ILogger<OpenDataGouvSource> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<(List<CompanySearchResultDto> Results, int TotalFound, int RawPageCount)> SearchAsync(
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
        if (request.OnlyIndividualEntrepreneurs) queryParts.Add("est_entrepreneur_individuel=true");
        if (request.NatureJuridiqueCodes is { Count: > 0 })
            queryParts.Add($"nature_juridique={Uri.EscapeDataString(string.Join(",", request.NatureJuridiqueCodes))}");
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
            return (new List<CompanySearchResultDto>(), 0, 0);
        }

        if (response?.Results == null) return (new List<CompanySearchResultDto>(), 0, 0);

        var rawPageCount = response.Results.Count;

        var results = response.Results
            .Select(MapToDto)
            // On ne rejette que les lignes inexploitables : ni nom, ni SIREN
            .Where(r => !string.IsNullOrWhiteSpace(r.CompanyName) || !string.IsNullOrWhiteSpace(r.Siren))
            .ToList();

        // Filtrage entrepreneurs individuels côté client
        if (!request.IncludeIndividualEntrepreneurs)
            results = results.Where(r => !r.IsIndividualEntrepreneur).ToList();

        // Filtrage établissements sans salarié côté client
        if (!request.IncludeNoEmployees)
            results = results.Where(r => r.EmployeeCount is > 0).ToList();

        // Filtrage date de création côté client (non supporté par l'API)
        if (request.CreatedAfter is not null)
            results = results.Where(r => r.CreationDate is null || r.CreationDate >= request.CreatedAfter).ToList();
        if (request.CreatedBefore is not null)
            results = results.Where(r => r.CreationDate is null || r.CreationDate <= request.CreatedBefore).ToList();

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

        return (results, response.TotalResults, rawPageCount);
    }

    private CompanySearchResultDto MapToDto(GouvCompany c)
    {
        var isNonDiffusible = c.StatutDiffusion is not null
            && !c.StatutDiffusion.Equals("O", StringComparison.OrdinalIgnoreCase);

        var (firstName, lastName) = ExtractEntrepreneurName(c);
        var tradeName = FirstNonEmpty(c.Siege?.ListeEnseignes?.FirstOrDefault(), c.Siege?.NomCommercial);
        var displayName = ResolveDisplayName(c, tradeName, firstName, lastName, isNonDiffusible);

        return new CompanySearchResultDto
        {
            CompanyName = displayName,
            TradeName = tradeName,
            Siren = c.Siren,
            Siret = c.Siege?.Siret ?? c.Siren,
            Industry = c.LibelleActivitePrincipale,
            NafCode = c.ActivitePrincipale,
            Address = FirstNonEmpty(c.Siege?.Adresse, c.Siege?.GeoAdresse),
            PostalCode = c.Siege?.CodePostal,
            City = c.Siege?.LibelleCommune,
            Department = c.Siege?.Departement,
            Region = c.Siege?.Region,
            Latitude = TryParseDouble(c.Siege?.Latitude),
            Longitude = TryParseDouble(c.Siege?.Longitude),
            EmployeeCount = MapEmployeeCount(c.Siege?.TrancheEffectifSalarie ?? c.TrancheEffectifSalarie),
            IsActive = (c.EtatAdministratif ?? c.Siege?.EtatAdministratif) == "A",
            Provider = ProviderName,
            IsIndividualEntrepreneur = c.NatureJuridique == NatureJuridiqueEntrepreneurIndividuel,
            EntrepreneurFirstName = firstName,
            EntrepreneurLastName = lastName,
            NatureJuridique = c.NatureJuridique,
            CreationDate = TryParseDate(c.DateCreation),
            IsNonDiffusible = isNonDiffusible,
        };
    }

    /// <summary>
    /// Nom affiché, par ordre de priorité : enseigne → nom commercial →
    /// dénomination (nom_complet/sigle) → prénom+nom de l'entrepreneur →
    /// raison sociale → placeholder pour les non-diffusibles.
    /// </summary>
    private static string ResolveDisplayName(
        GouvCompany c, string? tradeName, string? firstName, string? lastName, bool isNonDiffusible)
    {
        if (!string.IsNullOrWhiteSpace(tradeName)) return tradeName;
        if (!string.IsNullOrWhiteSpace(c.NomComplet)) return c.NomComplet;
        if (!string.IsNullOrWhiteSpace(c.Sigle)) return c.Sigle;

        if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
            return $"{firstName} {lastName}".Trim();

        if (!string.IsNullOrWhiteSpace(c.NomRaisonSociale)) return c.NomRaisonSociale;

        return isNonDiffusible ? "Entrepreneur individuel (non diffusible)" : string.Empty;
    }

    private static (string? FirstName, string? LastName) ExtractEntrepreneurName(GouvCompany c)
    {
        var dirigeant = c.Dirigeants?.FirstOrDefault(d =>
            string.Equals(d.TypeDirigeant, "personne physique", StringComparison.OrdinalIgnoreCase));
        if (dirigeant is null) return (null, null);

        var firstName = TitleCase(dirigeant.Prenoms?.Split(',', ' ').FirstOrDefault());
        var lastName = TitleCase(dirigeant.Nom);
        return (firstName, lastName);
    }

    private static string? TitleCase(string? s)
        => string.IsNullOrWhiteSpace(s)
            ? null
            : CultureInfo.GetCultureInfo("fr-FR").TextInfo.ToTitleCase(s.Trim().ToLowerInvariant());

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static DateOnly? TryParseDate(string? value)
        => DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static double? TryParseDouble(string? value)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;

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

        [JsonPropertyName("sigle")]
        public string? Sigle { get; set; }

        [JsonPropertyName("siren")]
        public string? Siren { get; set; }

        [JsonPropertyName("nature_juridique")]
        public string? NatureJuridique { get; set; }

        [JsonPropertyName("date_creation")]
        public string? DateCreation { get; set; }

        [JsonPropertyName("statut_diffusion")]
        public string? StatutDiffusion { get; set; }

        [JsonPropertyName("activite_principale")]
        public string? ActivitePrincipale { get; set; }

        [JsonPropertyName("libelle_activite_principale")]
        public string? LibelleActivitePrincipale { get; set; }

        [JsonPropertyName("etat_administratif")]
        public string? EtatAdministratif { get; set; }

        [JsonPropertyName("tranche_effectif_salarie")]
        public string? TrancheEffectifSalarie { get; set; }

        [JsonPropertyName("dirigeants")]
        public List<GouvDirigeant>? Dirigeants { get; set; }

        [JsonPropertyName("siege")]
        public GouvSiege? Siege { get; set; }
    }

    private sealed class GouvDirigeant
    {
        [JsonPropertyName("nom")]
        public string? Nom { get; set; }

        [JsonPropertyName("prenoms")]
        public string? Prenoms { get; set; }

        [JsonPropertyName("denomination")]
        public string? Denomination { get; set; }

        [JsonPropertyName("qualite")]
        public string? Qualite { get; set; }

        [JsonPropertyName("type_dirigeant")]
        public string? TypeDirigeant { get; set; }
    }

    private sealed class GouvSiege
    {
        [JsonPropertyName("siret")]
        public string? Siret { get; set; }

        [JsonPropertyName("adresse")]
        public string? Adresse { get; set; }

        [JsonPropertyName("geo_adresse")]
        public string? GeoAdresse { get; set; }

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

        [JsonPropertyName("etat_administratif")]
        public string? EtatAdministratif { get; set; }

        [JsonPropertyName("liste_enseignes")]
        public List<string>? ListeEnseignes { get; set; }

        [JsonPropertyName("nom_commercial")]
        public string? NomCommercial { get; set; }
    }
}
