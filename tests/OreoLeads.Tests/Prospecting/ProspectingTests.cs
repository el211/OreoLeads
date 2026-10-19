using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Application.Common;
using OreoLeads.Application.Features.Prospecting;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Brevo;
using OreoLeads.Infrastructure.Enrichment;
using OreoLeads.Infrastructure.Fetching;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Tests.Prospecting;

public class ProspectingTests
{
    [Theory]
    [InlineData("HTTPS://WWW.Example.COM/contact?q=1", "example.com")]
    [InlineData("example.com/", "example.com")]
    [InlineData("https://shop.example.com", "shop.example.com")]
    [InlineData("file:///etc/passwd", null)]
    [InlineData("https://user:password@example.com", null)]
    public void DomainIdentityNormalizesWithoutMergingTenants(string url, string? expected)
        => Assert.Equal(expected, CompanyIdentity.Domain(url));

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("203.0.113.10")]
    [InlineData("224.0.0.1")]
    [InlineData("::")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("64:ff9b::a00:1")]
    public void ReservedAndMappedAddressesAreBlocked(string ip)
        => Assert.Throws<InvalidOperationException>(() => SsrfGuard.ValidateIp(IPAddress.Parse(ip)));

    [Theory]
    [InlineData("User-agent: *\nDisallow: /", "/contact", false)]
    [InlineData("User-agent: *\nDisallow: /private\nAllow: /private/contact", "/private/contact", true)]
    [InlineData("User-agent: *\nDisallow: /*?", "/contact?tracking=1", false)]
    [InlineData("User-agent: *\nDisallow: /\nUser-agent: OreoLeads\nAllow: /", "/contact", true)]
    [InlineData("User-agent: *\nDisallow:", "/contact", true)]
    public void RobotsRespectsSpecificGroupsAndLongestRules(string robots, string path, bool expected)
        => Assert.Equal(expected, RobotsPolicy.Allows(robots, path));

    [Fact]
    public void FailedCrawlDoesNotCreateWebsiteOpportunities()
    {
        var evidence = Qualification.Evaluate(new Lead(), new WebsiteAnalysis { AnalysisError = "Timeout", HttpStatus = 0 }, null);
        Assert.Empty(evidence);
    }

    [Fact]
    public void WebsiteEvidenceHasSourceAndDoesNotClaimMobileFailure()
    {
        var evidence = Qualification.Evaluate(new Lead(), new WebsiteAnalysis
        { Url = "https://example.com", HttpStatus = 200, PageTitle = "Company", HasViewport = false, UsesHttps = true }, null);
        Assert.Contains(evidence, e => e.Dimension == "Website" && e.SourceUrl == "https://example.com" && e.Observation.Contains("needs review"));
        Assert.InRange(evidence.Sum(e => e.Points), 0, 100);
    }

    [Fact]
    public async Task PrivateCrawlFailsBeforeNetworkAccess()
    {
        var result = await new HttpPageFetcher(NullLogger<HttpPageFetcher>.Instance).FetchAsync("http://127.0.0.1/admin");
        Assert.NotNull(result.Error);
        Assert.Null(result.Html);
    }

    [Fact]
    public void ContactParserOnlyAcceptsExplicitPersonRecords()
    {
        using var json = JsonDocument.Parse("""{"@graph":[{"@type":"Organization","name":"A"},{"@type":"Person","name":"Jane Doe","jobTitle":"CEO"}]}""");
        var people = ContactDiscoveryService.People(json.RootElement).ToList();
        Assert.Single(people);
        Assert.Equal("Jane Doe", people[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task SuppressionOverridesApprovalAndIsCaseInsensitive()
    {
        await using var db = Database();
        var draft = new GeneratedEmail { Status = EmailStatus.Approved, Subject = "Hello", Body = "Body" };
        db.GeneratedEmails.Add(draft);
        db.UnsubscribeRecords.Add(new() { Email = " SALES@EXAMPLE.COM " });
        await db.SaveChangesAsync();
        var job = new EmailSendJob { GeneratedEmailId = draft.Id, LeadId = draft.LeadId, ToEmail = "sales@example.com", Subject = "Hello", HtmlBody = "Body" };
        Assert.Contains("suppressed", await OutreachGuard.BlockReasonAsync(db, job, default));
    }

    [Fact]
    public async Task ChangedDraftCannotSendPreviouslyApprovedContent()
    {
        await using var db = Database();
        var draft = new GeneratedEmail { Status = EmailStatus.Approved, Subject = "New", Body = "Body" };
        db.GeneratedEmails.Add(draft);
        await db.SaveChangesAsync();
        var job = new EmailSendJob { GeneratedEmailId = draft.Id, LeadId = draft.LeadId, Subject = "Old", HtmlBody = "Body" };
        Assert.Contains("changed", await OutreachGuard.BlockReasonAsync(db, job, default));
    }

    [Fact]
    public async Task CampaignsAreTenantFilteredAndWebsiteDomainIsPersisted()
    {
        var tenant = new TenantContext();
        var org = Guid.NewGuid();
        tenant.SetOrganization(org);
        await using var db = Database(tenant);
        db.ProspectingCampaigns.AddRange(new() { OrganizationId = org, Name = "Visible" }, new() { OrganizationId = Guid.NewGuid(), Name = "Other" });
        var lead = new Lead { Website = "https://www.example.com/contact", OrganizationId = org };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();
        Assert.Single(await db.ProspectingCampaigns.ToListAsync());
        Assert.Equal("example.com", lead.WebsiteDomain);
    }

    private static ApplicationDbContext Database(TenantContext? tenant = null) => new(
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
}
