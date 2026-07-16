using FluentAssertions;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Tests.Domain;

public class LeadEntityTests
{
    [Fact]
    public void New_lead_should_have_default_values()
    {
        var lead = new Lead { CompanyName = "Test Corp" };

        lead.Id.Should().NotBeEmpty();
        lead.Status.Should().Be(LeadStatus.New);
        lead.Priority.Should().Be(LeadPriority.Medium);
        lead.Score.Should().Be(0);
        lead.Country.Should().Be("France");
        lead.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        lead.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void SetUpdatedAt_should_set_timestamp()
    {
        var lead = new Lead { CompanyName = "Test" };
        lead.SetUpdatedAt();
        lead.UpdatedAt.Should().NotBeNull();
        lead.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Lead_should_have_unique_id()
    {
        var lead1 = new Lead { CompanyName = "A" };
        var lead2 = new Lead { CompanyName = "B" };
        lead1.Id.Should().NotBe(lead2.Id);
    }

    [Fact]
    public void Lead_collections_should_be_initialized()
    {
        var lead = new Lead { CompanyName = "Test" };
        lead.Activities.Should().NotBeNull();
        lead.Notes.Should().NotBeNull();
        lead.FollowUps.Should().NotBeNull();
        lead.LeadTags.Should().NotBeNull();
    }

    [Fact]
    public void FollowUp_should_have_default_pending_status()
    {
        var followUp = new FollowUp
        {
            LeadId = Guid.NewGuid(),
            ScheduledAt = DateTime.UtcNow.AddDays(3)
        };
        followUp.Status.Should().Be(FollowUpStatus.Pending);
        followUp.Priority.Should().Be(LeadPriority.Medium);
    }

    [Fact]
    public void Tag_should_have_default_color()
    {
        var tag = new Tag { Name = "Test" };
        tag.Color.Should().Be("#6366f1");
    }

    [Fact]
    public void LeadNote_should_have_default_not_deleted()
    {
        var note = new LeadNote { Title = "Test", Content = "Content", LeadId = Guid.NewGuid() };
        note.IsDeleted.Should().BeFalse();
    }
}
