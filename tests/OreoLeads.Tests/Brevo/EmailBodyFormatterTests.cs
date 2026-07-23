using FluentAssertions;
using OreoLeads.Infrastructure.Services;

namespace OreoLeads.Tests.Brevo;

public class EmailBodyFormatterTests
{
    [Fact]
    public void EnsureHtml_PlainTextParagraphs_ConvertsToHtmlParagraphs()
    {
        var body = "Bonjour,\n\nJe me permets de vous contacter.\nDeuxième ligne.\n\nCordialement,\nElias";

        var html = EmailBodyFormatter.EnsureHtml(body);

        html.Should().Contain("<p style=\"margin:0 0 1em 0;\">Bonjour,</p>");
        html.Should().Contain("Je me permets de vous contacter.<br/>Deuxi&#232;me ligne.");
        html.Should().Contain("Cordialement,<br/>Elias");
    }

    [Fact]
    public void EnsureHtml_WindowsLineEndings_AreNormalized()
    {
        var html = EmailBodyFormatter.EnsureHtml("Ligne 1\r\n\r\nLigne 2");

        html.Should().Be("<p style=\"margin:0 0 1em 0;\">Ligne 1</p><p style=\"margin:0 0 1em 0;\">Ligne 2</p>");
    }

    [Fact]
    public void EnsureHtml_ExistingHtml_IsLeftUntouched()
    {
        var body = "<p>Déjà du HTML</p>";

        EmailBodyFormatter.EnsureHtml(body).Should().Be(body);
    }

    [Fact]
    public void EnsureHtml_SpecialCharacters_AreEncoded()
    {
        var html = EmailBodyFormatter.EnsureHtml("Prix < 100 € & livraison > 2 jours");

        html.Should().Contain("Prix &lt; 100 ").And.Contain("&amp; livraison &gt; 2 jours");
    }

    [Fact]
    public void EnsureHtml_EmptyBody_ReturnsAsIs()
    {
        EmailBodyFormatter.EnsureHtml("").Should().Be("");
    }
}
