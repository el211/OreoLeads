using FluentAssertions;
using OreoLeads.Infrastructure.Automation;

namespace OreoLeads.Tests.Automation;

public class AutomationTemplateTests
{
    [Fact]
    public async Task GetTemplates_ReturnsBuiltIn()
    {
        var db = AutomationTestHelpers.CreateDbContext();
        var template = AutomationTestHelpers.CreateTemplate();
        db.AutomationTemplates.Add(template);
        await db.SaveChangesAsync();

        var svc = new AutomationWorkflowService(db);
        var templates = await svc.GetTemplatesAsync();

        templates.Should().HaveCount(1);
        templates[0].IsBuiltIn.Should().BeTrue();
    }

    [Fact]
    public async Task UseTemplate_CreatesWorkflow()
    {
        var db = AutomationTestHelpers.CreateDbContext();
        var template = AutomationTestHelpers.CreateTemplate("Bienvenue");
        db.AutomationTemplates.Add(template);
        await db.SaveChangesAsync();

        var svc = new AutomationWorkflowService(db);
        var workflow = await svc.UseTemplateAsync(template.Id, null);

        workflow.Should().NotBeNull();
        workflow.Name.Should().Be("Bienvenue");
    }

    [Fact]
    public async Task Template_Welcome_HasCorrectTrigger()
    {
        var db = AutomationTestHelpers.CreateDbContext();
        var svc = new AutomationWorkflowService(db);
        await svc.SeedBuiltInTemplatesAsync();

        var templates = await svc.GetTemplatesAsync();
        var welcome = templates.FirstOrDefault(t => t.Name == "Bienvenue");

        welcome.Should().NotBeNull();
        welcome!.TriggerJson.Should().Contain("LeadCreated");
    }

    [Fact]
    public async Task Template_Prospection_HasMultipleActions()
    {
        var db = AutomationTestHelpers.CreateDbContext();
        var svc = new AutomationWorkflowService(db);
        await svc.SeedBuiltInTemplatesAsync();

        var templates = await svc.GetTemplatesAsync();
        var prospection = templates.FirstOrDefault(t => t.Name == "Prospection");

        prospection.Should().NotBeNull();
        prospection!.ActionsJson.Should().Contain("Wait");
        prospection.ActionsJson.Should().Contain("SendEmail");
    }
}
