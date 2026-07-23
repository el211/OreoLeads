using FluentAssertions;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Brevo;

namespace OreoLeads.Tests.Brevo;

public class EmailStatusProgressionTests
{
    [Theory]
    [InlineData(EmailType.FirstContact, LeadStatus.EmailSent)]
    [InlineData(EmailType.FollowUp, LeadStatus.FollowUp1)]
    [InlineData(EmailType.LastFollowUp, LeadStatus.FollowUp2)]
    [InlineData(EmailType.Proposal, LeadStatus.ProposalSent)]
    public void MapEmailTypeToStatus_ReturnsTarget(EmailType type, LeadStatus expected)
    {
        EmailSendBackgroundService.MapEmailTypeToStatus(type).Should().Be(expected);
    }

    [Theory]
    [InlineData(EmailType.Reply)]
    [InlineData(EmailType.AfterMeeting)]
    public void MapEmailTypeToStatus_NoStatusChange_ForNeutralTypes(EmailType type)
    {
        EmailSendBackgroundService.MapEmailTypeToStatus(type).Should().BeNull();
    }

    // La progression est "avant uniquement" : une relance (FollowUp1) est plus
    // avancée qu'EmailSent, donc le statut évolue bien lors d'un e-mail de relance.
    [Fact]
    public void FollowUp_IsAheadOfEmailSent()
    {
        ((int)LeadStatus.FollowUp1).Should().BeGreaterThan((int)LeadStatus.EmailSent);
        ((int)LeadStatus.FollowUp2).Should().BeGreaterThan((int)LeadStatus.FollowUp1);
    }
}
