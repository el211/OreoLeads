using FluentAssertions;
using OreoLeads.Infrastructure.Analysis;

namespace OreoLeads.Tests.Analysis;

/// <summary>
/// Tests de l'analyse HTML — sans aucune dépendance réseau.
/// Le HTML est fourni directement comme chaîne mockée.
/// </summary>
public class HtmlAnalyzerTests
{
    // ── ExtractTitle ──────────────────────────────────────────────────────────

    [Fact]
    public void ExtractTitle_ReturnsTitle_WhenPresent()
    {
        var html = "<html><head><title>Restaurant Le Petit Prince</title></head></html>";
        HtmlAnalyzer.ExtractTitle(html).Should().Be("Restaurant Le Petit Prince");
    }

    [Fact]
    public void ExtractTitle_TrimsWhitespace()
    {
        var html = "<title>  Mon Site  </title>";
        HtmlAnalyzer.ExtractTitle(html).Should().Be("Mon Site");
    }

    [Fact]
    public void ExtractTitle_ReturnsNull_WhenAbsent()
    {
        HtmlAnalyzer.ExtractTitle("<html><body>no title</body></html>").Should().BeNull();
    }

    // ── ExtractMetaDescription ────────────────────────────────────────────────

    [Fact]
    public void ExtractMetaDescription_ReturnsDescription_StandardOrder()
    {
        var html = @"<meta name=""description"" content=""Bienvenue chez nous"" />";
        HtmlAnalyzer.ExtractMetaDescription(html).Should().Be("Bienvenue chez nous");
    }

    [Fact]
    public void ExtractMetaDescription_ReturnsDescription_ReversedOrder()
    {
        var html = @"<meta content=""Notre boutique en ligne"" name=""description"" />";
        HtmlAnalyzer.ExtractMetaDescription(html).Should().Be("Notre boutique en ligne");
    }

    [Fact]
    public void ExtractMetaDescription_ReturnsNull_WhenAbsent()
    {
        HtmlAnalyzer.ExtractMetaDescription("<html><body>no meta</body></html>").Should().BeNull();
    }

    // ── HasViewport ───────────────────────────────────────────────────────────

    [Fact]
    public void HasViewport_True_WhenViewportMetaPresent()
    {
        var html = @"<meta name=""viewport"" content=""width=device-width, initial-scale=1"" />";
        HtmlAnalyzer.HasViewport(html).Should().BeTrue();
    }

    [Fact]
    public void HasViewport_False_WhenAbsent()
    {
        HtmlAnalyzer.HasViewport("<html><head></head></html>").Should().BeFalse();
    }

    // ── HasContactForm ────────────────────────────────────────────────────────

    [Fact]
    public void HasContactForm_True_WhenFormHasEmailField()
    {
        var html = @"<form action=""/contact"">
                       <input type=""email"" name=""email"" />
                       <textarea name=""message""></textarea>
                       <button>Envoyer</button>
                     </form>";
        HtmlAnalyzer.HasContactForm(html).Should().BeTrue();
    }

    [Fact]
    public void HasContactForm_False_WhenFormIsSearchOnly()
    {
        var html = @"<form action=""/search""><input type=""text"" name=""q"" /><button>Rechercher</button></form>";
        HtmlAnalyzer.HasContactForm(html).Should().BeFalse();
    }

    // ── HasQuoteForm ──────────────────────────────────────────────────────────

    [Fact]
    public void HasQuoteForm_True_WhenDevisKeyword()
    {
        var html = "<p>Demandez votre devis gratuit en ligne.</p>";
        HtmlAnalyzer.HasQuoteForm(html).Should().BeTrue();
    }

    [Fact]
    public void HasQuoteForm_False_WhenKeywordAbsent()
    {
        HtmlAnalyzer.HasQuoteForm("<p>Bienvenue sur notre site.</p>").Should().BeFalse();
    }

    // ── HasBookingSystem ──────────────────────────────────────────────────────

    [Fact]
    public void HasBooking_True_WhenReservationKeyword()
    {
        var html = @"<a href=""/reservation"">Réserver une table</a>";
        HtmlAnalyzer.HasBookingSystem(html).Should().BeTrue();
    }

    // ── HasEmailVisible ───────────────────────────────────────────────────────

    [Fact]
    public void HasEmailVisible_True_WhenMailtoLink()
    {
        var html = @"<a href=""mailto:contact@monsite.fr"">Contactez-nous</a>";
        HtmlAnalyzer.HasEmailVisible(html).Should().BeTrue();
    }

    [Fact]
    public void HasEmailVisible_True_WhenEmailInText()
    {
        var html = "<p>Envoyez vos questions à contact@monsite.fr</p>";
        HtmlAnalyzer.HasEmailVisible(html).Should().BeTrue();
    }

    // ── HasPhoneVisible ───────────────────────────────────────────────────────

    [Fact]
    public void HasPhoneVisible_True_WhenFrenchPhone()
    {
        var html = "<p>Appelez-nous au 01 23 45 67 89</p>";
        HtmlAnalyzer.HasPhoneVisible(html).Should().BeTrue();
    }

    [Fact]
    public void HasPhoneVisible_True_WhenTelLink()
    {
        var html = @"<a href=""tel:+33612345678"">Appeler</a>";
        HtmlAnalyzer.HasPhoneVisible(html).Should().BeTrue();
    }

    // ── HasPrivacyPolicy ──────────────────────────────────────────────────────

    [Fact]
    public void HasPrivacyPolicy_True_WhenKeywordPresent()
    {
        var html = @"<a href=""/rgpd"">Politique de confidentialité</a>";
        HtmlAnalyzer.HasPrivacyPolicy(html).Should().BeTrue();
    }

    // ── HasLegalNotice ────────────────────────────────────────────────────────

    [Fact]
    public void HasLegalNotice_True_WhenMentionsLegales()
    {
        var html = @"<a href=""/mentions-legales"">Mentions légales</a>";
        HtmlAnalyzer.HasLegalNotice(html).Should().BeTrue();
    }

    [Fact]
    public void HasLegalNotice_False_WhenAbsent()
    {
        HtmlAnalyzer.HasLegalNotice("<html><body><p>Hello</p></body></html>").Should().BeFalse();
    }

    // ── HasAddressVisible ─────────────────────────────────────────────────────

    [Fact]
    public void HasAddressVisible_True_WhenPostalCode()
    {
        var html = "<p>Notre adresse : 12 rue des Fleurs, 75001 Paris</p>";
        HtmlAnalyzer.HasAddressVisible(html).Should().BeTrue();
    }
}
