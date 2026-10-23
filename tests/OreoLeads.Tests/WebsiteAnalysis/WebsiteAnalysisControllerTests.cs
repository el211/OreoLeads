using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Api.Controllers;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.WebsiteAnalysis.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Persistence.Repositories;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Tests.Analysis;

public class WebsiteAnalysisControllerTests
{
    [Fact]
    public async Task Analyze_RemainsVisibleOnNextRequest_AndIsIsolatedByOrganization()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var tenant = new TenantContext();
        tenant.SetOrganization(Guid.NewGuid());
        var lead = new Lead { CompanyName = "Restaurant", Website = "https://example.com", OrganizationId = tenant.OrganizationId };
        Guid analysisId;
        await using (var db = new ApplicationDbContext(options, tenant))
        {
            db.Leads.Add(lead);
            await db.SaveChangesAsync();
            var controller = CreateController(db);
            var response = Assert.IsType<OkObjectResult>(await controller.Analyze(lead.Id, default));
            analysisId = Assert.IsType<WebsiteAnalysisDto>(response.Value).Id;
        }
        await using (var db = new ApplicationDbContext(options, tenant))
        {
            var controller = CreateController(db);
            var response = Assert.IsType<OkObjectResult>(await controller.GetLatest(lead.Id, default));
            Assert.Equal(analysisId, Assert.IsType<WebsiteAnalysisDto>(response.Value).Id);
            var history = Assert.IsType<OkObjectResult>(await controller.GetHistory(lead.Id, default));
            Assert.Single(Assert.IsType<List<WebsiteAnalysisDto>>(history.Value));
            Assert.Equal(tenant.OrganizationId, (await db.WebsiteAnalyses.SingleAsync()).OrganizationId);
        }
        var otherTenant = new TenantContext();
        otherTenant.SetOrganization(Guid.NewGuid());
        await using var otherDb = new ApplicationDbContext(options, otherTenant);
        Assert.Empty(await otherDb.WebsiteAnalyses.ToListAsync());
        Assert.IsType<NotFoundObjectResult>(await CreateController(otherDb).Analyze(lead.Id, default));
    }

    private static WebsiteAnalysisController CreateController(ApplicationDbContext db)
        => new(new Analyzer(), new LeadRepository(db), db);

    private sealed class Analyzer : IWebsiteAnalyzerService
    {
        public Task<OreoLeads.Domain.Entities.WebsiteAnalysis> AnalyzeAsync(Guid leadId, string url, CancellationToken ct = default)
            => Task.FromResult(new OreoLeads.Domain.Entities.WebsiteAnalysis { LeadId = leadId, Url = url });

        public OreoLeads.Domain.Entities.WebsiteAnalysis Recalculate(OreoLeads.Domain.Entities.WebsiteAnalysis existing) => existing;

        public WebsiteAnalysisDto ToDto(OreoLeads.Domain.Entities.WebsiteAnalysis analysis, string? industry = null)
            => new() { Id = analysis.Id, LeadId = analysis.LeadId, Url = analysis.Url };
    }
}
