using FluentAssertions;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Automation;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Automation;

public class AutomationMultiTenantTests
{
    private static readonly Guid Org1 = Guid.NewGuid();
    private static readonly Guid Org2 = Guid.NewGuid();

    [Fact]
    public async Task GetWorkflows_OtherOrg_ReturnsEmpty()
    {
        var tenant = new TenantContext();
        tenant.SetOrganization(Org1);
        var db = AutomationTestHelpers.CreateDbContext(tenant);

        var wf = AutomationTestHelpers.CreateWorkflow(orgId: Org2);
        db.AutomationWorkflows.Add(wf);
        await db.SaveChangesAsync();

        var svc = new AutomationWorkflowService(db);
        var workflows = await svc.GetWorkflowsAsync(Org1);

        workflows.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_OtherOrgWorkflow_Denied()
    {
        var tenant = new TenantContext();
        tenant.SetOrganization(Org1);
        var db = AutomationTestHelpers.CreateDbContext(tenant);

        // Workflow belongs to Org2 -> not visible to Org1
        var wf = AutomationTestHelpers.CreateWorkflow(orgId: Org2);
        db.AutomationWorkflows.Add(wf);
        await db.SaveChangesAsync();

        var svc = new AutomationWorkflowService(db);
        var result = await svc.GetWorkflowAsync(wf.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetExecutions_OtherOrg_ReturnsEmpty()
    {
        var tenant = new TenantContext();
        tenant.SetOrganization(Org1);
        var db = AutomationTestHelpers.CreateDbContext(tenant);

        var wf = AutomationTestHelpers.CreateWorkflow(orgId: Org2);
        db.AutomationWorkflows.Add(wf);

        var exec = AutomationTestHelpers.CreateExecution(wf.Id, orgId: Org2);
        db.AutomationExecutions.Add(exec);
        await db.SaveChangesAsync();

        var svc = new AutomationWorkflowService(db);
        var executions = await svc.GetExecutionsAsync(null, Org1);

        executions.Should().BeEmpty();
    }

    [Fact]
    public async Task Templates_BuiltIn_VisibleToAllOrgs()
    {
        // No tenant filter should show built-in templates
        var tenant = new TenantContext();
        tenant.SetOrganization(Org1);
        var db = AutomationTestHelpers.CreateDbContext(tenant);

        var template = AutomationTestHelpers.CreateTemplate(isBuiltIn: true);
        db.AutomationTemplates.Add(template);
        await db.SaveChangesAsync();

        var svc = new AutomationWorkflowService(db);
        var templates = await svc.GetTemplatesAsync();

        templates.Should().HaveCount(1);
    }
}
