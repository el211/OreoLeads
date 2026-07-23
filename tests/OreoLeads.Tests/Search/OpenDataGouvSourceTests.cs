using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Application.Features.Search.DTOs;
using OreoLeads.Infrastructure.Sources;

namespace OreoLeads.Tests.Search;

public class OpenDataGouvSourceTests
{
    // ── EI avec enseigne : l'enseigne prime sur tout ──────────────────────────
    [Fact]
    public async Task Maps_EI_WithEnseigne_UsesEnseigneAsName()
    {
        var json = Wrap("""
            {
              "siren": "111222333",
              "nom_complet": "DUPONT JEAN",
              "nature_juridique": "1000",
              "date_creation": "2020-05-01",
              "statut_diffusion": "O",
              "dirigeants": [ { "nom": "DUPONT", "prenoms": "Jean", "type_dirigeant": "personne physique" } ],
              "siege": { "siret": "11122233300012", "liste_enseignes": ["BOULANGERIE DU COIN"], "libelle_commune": "LYON" }
            }
            """);

        var result = (await Build(json).SearchAsync(new CompanySearchRequestDto())).Results.Single();

        result.CompanyName.Should().Be("BOULANGERIE DU COIN");
        result.IsIndividualEntrepreneur.Should().BeTrue();
        result.EntrepreneurFirstName.Should().Be("Jean");
        result.EntrepreneurLastName.Should().Be("Dupont");
    }

    // ── EI sans enseigne : repli sur nom_commercial ───────────────────────────
    [Fact]
    public async Task Maps_EI_WithNomCommercialOnly_UsesNomCommercial()
    {
        var json = Wrap("""
            {
              "siren": "111222333",
              "nature_juridique": "1000",
              "siege": { "nom_commercial": "Chez Marie Coiffure", "libelle_commune": "PARIS" }
            }
            """);

        var result = (await Build(json).SearchAsync(new CompanySearchRequestDto())).Results.Single();
        result.CompanyName.Should().Be("Chez Marie Coiffure");
    }

    // ── EI dont le nom vient des dirigeants (pas d'enseigne, pas de nom_complet) ─
    [Fact]
    public async Task Maps_EI_FromDirigeantName_WhenNoOtherName()
    {
        var json = Wrap("""
            {
              "siren": "111222333",
              "nature_juridique": "1000",
              "dirigeants": [ { "nom": "MARTIN", "prenoms": "Sophie, Anne", "type_dirigeant": "personne physique" } ],
              "siege": { "libelle_commune": "MARSEILLE" }
            }
            """);

        var result = (await Build(json).SearchAsync(new CompanySearchRequestDto())).Results.Single();
        result.CompanyName.Should().Be("Sophie Martin");
        result.EntrepreneurFirstName.Should().Be("Sophie");
    }

    // ── Non-diffusible : placeholder conservé, drapeau posé ───────────────────
    [Fact]
    public async Task Maps_NonDiffusible_KeepsPlaceholderAndFlag()
    {
        var json = Wrap("""
            {
              "siren": "111222333",
              "nature_juridique": "1000",
              "statut_diffusion": "P",
              "siege": { }
            }
            """);

        var result = (await Build(json).SearchAsync(new CompanySearchRequestDto())).Results.Single();
        result.IsNonDiffusible.Should().BeTrue();
        result.CompanyName.Should().Be("Entrepreneur individuel (non diffusible)");
    }

    // ── Société classique inchangée ───────────────────────────────────────────
    [Fact]
    public async Task Maps_ClassicCompany_UsesNomComplet()
    {
        var json = Wrap("""
            {
              "siren": "444555666",
              "nom_complet": "OREO STUDIOS SAS",
              "nature_juridique": "5710",
              "siege": { "adresse": "10 RUE DE PARIS 69001 LYON", "libelle_commune": "LYON" }
            }
            """);

        var result = (await Build(json).SearchAsync(new CompanySearchRequestDto())).Results.Single();
        result.CompanyName.Should().Be("OREO STUDIOS SAS");
        result.IsIndividualEntrepreneur.Should().BeFalse();
        result.Address.Should().Be("10 RUE DE PARIS 69001 LYON");
    }

    // ── Paramètres de requête EI transmis à l'API ─────────────────────────────
    [Fact]
    public async Task SendsIndividualEntrepreneurParams_InOutgoingUrl()
    {
        string? capturedUrl = null;
        var handler = new CapturingHandler(req => capturedUrl = req.RequestUri!.ToString(), Wrap("{}"));
        var source = new OpenDataGouvSource(new HttpClient(handler), NullLogger<OpenDataGouvSource>.Instance);

        await source.SearchAsync(new CompanySearchRequestDto
        {
            OnlyIndividualEntrepreneurs = true,
            NatureJuridiqueCodes = ["1000", "5410"],
        });

        capturedUrl.Should().Contain("est_entrepreneur_individuel=true");
        capturedUrl.Should().Contain("nature_juridique=1000%2C5410");
    }

    // ── Filtre sans salarié côté client ───────────────────────────────────────
    [Fact]
    public async Task ExcludesNoEmployees_WhenIncludeNoEmployeesFalse()
    {
        var json = Wrap("""
            { "siren": "111", "nom_complet": "Zero Salarié", "siege": { "tranche_effectif_salarie": "00" } },
            { "siren": "222", "nom_complet": "Avec Salariés", "siege": { "tranche_effectif_salarie": "11" } }
            """);

        var results = (await Build(json).SearchAsync(new CompanySearchRequestDto { IncludeNoEmployees = false })).Results;

        results.Should().ContainSingle().Which.CompanyName.Should().Be("Avec Salariés");
    }

    // ── RawPageCount reflète les résultats bruts avant filtres ────────────────
    [Fact]
    public async Task ReturnsRawPageCount_BeforeClientFilters()
    {
        var json = Wrap("""
            { "siren": "111", "nom_complet": "A", "siege": { "tranche_effectif_salarie": "00" } },
            { "siren": "222", "nom_complet": "B", "siege": { "tranche_effectif_salarie": "00" } }
            """);

        var (results, _, rawPageCount) = await Build(json).SearchAsync(
            new CompanySearchRequestDto { IncludeNoEmployees = false });

        rawPageCount.Should().Be(2);
        results.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Wrap(string resultsCsv)
        => $$"""{ "results": [ {{resultsCsv}} ], "total_results": 2, "page": 1, "per_page": 25, "total_pages": 1 }""";

    private static OpenDataGouvSource Build(string json)
    {
        var handler = new CapturingHandler(_ => { }, json);
        return new OpenDataGouvSource(new HttpClient(handler), NullLogger<OpenDataGouvSource>.Instance);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Action<HttpRequestMessage> _capture;
        private readonly string _body;

        public CapturingHandler(Action<HttpRequestMessage> capture, string body)
        {
            _capture = capture;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _capture(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json"),
            });
        }
    }
}
