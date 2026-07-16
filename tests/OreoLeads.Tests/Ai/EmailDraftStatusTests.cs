using FluentAssertions;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Tests.Ai;

public class EmailDraftStatusTests
{
    [Fact]
    public void NewDraft_HasGeneratedStatus()
    {
        var draft = new GeneratedEmail();
        draft.Status.Should().Be(EmailStatus.Generated);
    }

    [Fact]
    public void NewDraft_HasVersion1()
    {
        var draft = new GeneratedEmail();
        draft.CurrentVersion.Should().Be(1);
    }

    [Fact]
    public void AllStatusValues_Exist()
    {
        var values = Enum.GetValues<EmailStatus>();
        values.Should().Contain(EmailStatus.Generated);
        values.Should().Contain(EmailStatus.Edited);
        values.Should().Contain(EmailStatus.Approved);
        values.Should().Contain(EmailStatus.Rejected);
        values.Should().Contain(EmailStatus.Sent);
    }

    [Fact]
    public void GeneratedEmail_DefaultStyle_IsProfessional()
    {
        var draft = new GeneratedEmail();
        draft.Style.Should().Be(EmailStyle.Professional);
    }

    [Fact]
    public void GeneratedEmail_DefaultLength_IsNormal()
    {
        var draft = new GeneratedEmail();
        draft.Length.Should().Be(EmailLength.Normal);
    }

    [Fact]
    public void GeneratedEmail_DefaultType_IsFirstContact()
    {
        var draft = new GeneratedEmail();
        draft.Type.Should().Be(EmailType.FirstContact);
    }

    [Fact]
    public void EmailDraftVersion_HasCorrectEntityStructure()
    {
        var version = new EmailDraftVersion
        {
            Version      = 2,
            Subject      = "Subject v2",
            Body         = "Body v2",
            ProviderUsed = "MockProvider",
            ModelUsed    = "mock-1.0",
            GenerationMs = 100
        };

        version.Version.Should().Be(2);
        version.Subject.Should().Be("Subject v2");
    }

    [Fact]
    public void AiConfiguration_DefaultValues_AreCorrect()
    {
        var config = new AiConfiguration();
        config.ProviderType.Should().Be(AiProviderType.Claude);
        config.Temperature.Should().Be(0.7f);
        config.MaxTokens.Should().Be(1000);
        config.TimeoutSeconds.Should().Be(30);
        config.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void PromptTemplate_IsSystem_ByDefault()
    {
        var t = new PromptTemplate();
        t.IsSystem.Should().BeTrue();
    }

    [Fact]
    public void AllAiProviderTypes_Exist()
    {
        var values = Enum.GetValues<AiProviderType>();
        values.Should().Contain(AiProviderType.Claude);
        values.Should().Contain(AiProviderType.OpenAI);
        values.Should().Contain(AiProviderType.Ollama);
        values.Should().Contain(AiProviderType.GenericOpenAI);
    }

    [Fact]
    public void AllEmailStyles_Exist()
    {
        var values = Enum.GetValues<EmailStyle>();
        values.Should().Contain(EmailStyle.Professional);
        values.Should().Contain(EmailStyle.Friendly);
        values.Should().Contain(EmailStyle.Premium);
        values.Should().Contain(EmailStyle.Short);
        values.Should().Contain(EmailStyle.Luxury);
        values.Should().Contain(EmailStyle.French);
        values.Should().Contain(EmailStyle.English);
    }

    [Fact]
    public void AllEmailTypes_Exist()
    {
        var values = Enum.GetValues<EmailType>();
        values.Should().Contain(EmailType.FirstContact);
        values.Should().Contain(EmailType.FollowUp);
        values.Should().Contain(EmailType.Reply);
        values.Should().Contain(EmailType.Proposal);
        values.Should().Contain(EmailType.AfterMeeting);
        values.Should().Contain(EmailType.LastFollowUp);
    }
}
