using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Organization;

/// <summary>
/// Verifies that the EF HasQueryFilter tenant isolation works correctly.
/// Org A should not see Org B's leads, and vice versa.
/// </summary>
public class TenantIsolationTests
{
    private static ApplicationDbContext CreateDbContext(TenantContext? tenant = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, tenant);
    }

    [Fact]
    public async Task Leads_AreIsolated_ByOrganizationId()
    {
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        // Seed data without tenant filter (null tenant)
        await using (var seedCtx = CreateDbContext())
        {
            seedCtx.Leads.AddRange(
                new Lead { CompanyName = "Org A Lead 1", OrganizationId = orgA, Status = LeadStatus.New, Priority = LeadPriority.Medium },
                new Lead { CompanyName = "Org A Lead 2", OrganizationId = orgA, Status = LeadStatus.New, Priority = LeadPriority.Medium },
                new Lead { CompanyName = "Org B Lead 1", OrganizationId = orgB, Status = LeadStatus.New, Priority = LeadPriority.Medium }
            );
            await seedCtx.SaveChangesAsync();
        }

        // The in-memory provider doesn't carry data between DbContext instances with different db names.
        // Re-seed using same db name approach:
        var dbName = Guid.NewGuid().ToString();
        var sharedOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // Seed
        await using (var seedCtx = new ApplicationDbContext(sharedOptions, tenant: null))
        {
            seedCtx.Leads.AddRange(
                new Lead { CompanyName = "Org A Lead 1", OrganizationId = orgA, Status = LeadStatus.New, Priority = LeadPriority.Medium },
                new Lead { CompanyName = "Org A Lead 2", OrganizationId = orgA, Status = LeadStatus.New, Priority = LeadPriority.Medium },
                new Lead { CompanyName = "Org B Lead 1", OrganizationId = orgB, Status = LeadStatus.New, Priority = LeadPriority.Medium }
            );
            await seedCtx.SaveChangesAsync();
        }

        // Query as org A
        var tenantA = new TenantContext();
        tenantA.SetOrganization(orgA);
        await using var ctxA = new ApplicationDbContext(sharedOptions, tenantA);
        var leadsA = await ctxA.Leads.ToListAsync();
        leadsA.Should().HaveCount(2);
        leadsA.Should().AllSatisfy(l => l.OrganizationId.Should().Be(orgA));

        // Query as org B
        var tenantB = new TenantContext();
        tenantB.SetOrganization(orgB);
        await using var ctxB = new ApplicationDbContext(sharedOptions, tenantB);
        var leadsB = await ctxB.Leads.ToListAsync();
        leadsB.Should().HaveCount(1);
        leadsB[0].CompanyName.Should().Be("Org B Lead 1");

        // Query with null tenant (no filter — sees all)
        await using var ctxAll = new ApplicationDbContext(sharedOptions, tenant: null);
        var allLeads = await ctxAll.Leads.ToListAsync();
        allLeads.Should().HaveCount(3);
    }

    [Fact]
    public async Task PromptTemplates_SystemTemplates_VisibleToAllTenants()
    {
        var orgA = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using (var seed = new ApplicationDbContext(opts, tenant: null))
        {
            seed.PromptTemplates.AddRange(
                new PromptTemplate { Name = "System Prompt", Key = "sys", Content = "...", IsSystem = true },
                new PromptTemplate { Name = "Org A Prompt", Key = "org-a", Content = "...", IsSystem = false, OrganizationId = orgA }
            );
            await seed.SaveChangesAsync();
        }

        var tenant = new TenantContext();
        tenant.SetOrganization(orgA);
        await using var ctx = new ApplicationDbContext(opts, tenant);

        var templates = await ctx.PromptTemplates.ToListAsync();
        templates.Should().HaveCount(2, because: "system templates bypass the org filter");
    }
}
