using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Enrichment;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Enrichment;

public class ContactDiscoveryTests
{
    [Theory]
    [InlineData("06 12 34 56 78", "+33612345678")]
    [InlineData("tel:+33 (0)3 88 12 34 56", "+33388123456")]
    [InlineData("0033712345678", "+33712345678")]
    [InlineData("833207376", null)]
    [InlineData("83320737600017", null)]
    public void NormalizesFrenchNumbersWithoutTurningIdentifiersIntoPhones(string input, string? expected)
        => Assert.Equal(expected, PhoneExtractor.Normalize(input));

    [Fact]
    public void ExtractsPublishedPhonesAndIgnoresScriptsAndIdentifiers()
    {
        var numbers = PhoneExtractor.Extract("""
            <a href="tel:0612345678">Appeler</a><p>03 88 12 34 56</p>
            <script type="application/ld+json">{"telephone":"+33712345678"}</script>
            <script>var id = '01 23 45 67 89';</script><p>SIRET 83320737600017</p>
            """);
        Assert.Equal(new[] { "+33612345678", "+33712345678", "+33388123456" }, numbers);
    }

    [Theory]
    [InlineData(false, null, "+33612345678")]
    [InlineData(true, null, null)]
    [InlineData(false, "+33388111111", "+33388111111")]
    public async Task InspectsCandidatesButOnlyFillsEmptyPhoneOnAcceptedSites(bool candidate, string? existing, string? expected)
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        const string site = "https://example.org/";
        var lead = new Lead { Website = candidate ? null : site, Phone = existing };
        var fetcher = new FakePageFetcher().Add(site, "<p>03 88 12 34 56</p>")
            .Add(site + "contact", "<a href='tel:0612345678'>Mobile</a>");
        var report = await new ContactDiscoveryService(fetcher, db).InspectAsync(lead, candidateWebsite: candidate ? site : null);
        Assert.Equal(expected, lead.Phone);
        using var json = JsonDocument.Parse(report);
        Assert.Contains(json.RootElement.EnumerateArray(), p => p.TryGetProperty("phones", out var phones) && phones.EnumerateArray().Any(n => n.GetString() == "+33612345678"));
        Assert.Equal(candidate, json.RootElement[0].GetProperty("candidateOnly").GetBoolean());
    }
}
