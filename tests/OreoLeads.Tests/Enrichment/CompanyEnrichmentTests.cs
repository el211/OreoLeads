using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.WebsiteAnalysis.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Enrichment;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Enrichment;

public class CompanyEnrichmentTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("https://annuaire-entreprises.data.gouv.fr/entreprise/123456789")]
    public async Task CandidateIsExploredButUnconfirmedContactsAreNotAutoApplied(string? oldWebsite)
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var lead = new Lead { Website = oldWebsite };
        var job = new LeadEnrichment { LeadId = lead.Id };
        db.Leads.Add(lead);
        db.LeadEnrichments.Add(job);
        await db.SaveChangesAsync();
        var emails = new Emails();
        var contacts = new Contacts();
        var service = new CompanyEnrichmentService(db, new Websites(), emails, new Analyzer(),
            Options.Create(new EnrichmentSettings()), NullLogger<CompanyEnrichmentService>.Instance, contacts);
        Assert.Equal(EnrichmentStatus.NeedsReview, await service.RunAsync(job.Id));
        Assert.Equal("https://example.org/", emails.Scanned);
        Assert.Equal("https://example.org/", contacts.Scanned);
        Assert.Null(lead.Website);
        Assert.Null(lead.Email);
        Assert.Equal("contact@example.org", job.DiscoveredEmail);
        Assert.Equal(.65, job.EmailConfidence);
    }

    private sealed class Websites : IWebsiteDiscoveryService
    {
        public bool IsConfigured => true;
        public Task<WebsiteDiscoveryResult> DiscoverAsync(Lead lead, CancellationToken ct = default)
            => Task.FromResult(new WebsiteDiscoveryResult("https://example.org/", .65, [], [], [], 1));
    }
    private sealed class Emails : IEmailDiscoveryService
    {
        public string? Scanned { get; private set; }
        public Task<EmailDiscoveryResult> DiscoverFromWebsiteAsync(string url, CancellationToken ct = default)
        {
            Scanned = url;
            return Task.FromResult(new EmailDiscoveryResult("contact@example.org", url, "Website", DiscoveredEmailKind.Generic, .95));
        }
        public Task<EmailDiscoveryResult> DiscoverFromExternalProfilesAsync(IReadOnlyList<ExternalProfile> profiles, CancellationToken ct = default)
            => throw new InvalidOperationException("The candidate site must be inspected.");
    }
    private sealed class Contacts : IContactDiscoveryService
    {
        public string? Scanned { get; private set; }
        public Task<string> InspectAsync(Lead lead, CancellationToken ct = default, string? candidateWebsite = null)
        { Scanned = candidateWebsite; return Task.FromResult("[]"); }
    }
    private sealed class Analyzer : IWebsiteAnalyzerService
    {
        public Task<WebsiteAnalysis> AnalyzeAsync(Guid leadId, string url, CancellationToken ct = default) => throw new InvalidOperationException("Candidate is not an accepted website.");
        public WebsiteAnalysis Recalculate(WebsiteAnalysis existing) => existing;
        public WebsiteAnalysisDto ToDto(WebsiteAnalysis analysis, string? industry = null) => throw new NotSupportedException();
    }
}
