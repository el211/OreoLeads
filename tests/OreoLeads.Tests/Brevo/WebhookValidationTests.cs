using FluentAssertions;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Brevo;

namespace OreoLeads.Tests.Brevo;

public class WebhookValidationTests
{
    // ── 1. ValidWebhookKey_Passes ─────────────────────────────────────────────

    [Fact]
    public void ValidWebhookKey_Passes()
    {
        var act = () => WebhookValidator.Validate("my-secret", "my-secret");
        act.Should().NotThrow();
    }

    // ── 2. InvalidWebhookKey_Throws ───────────────────────────────────────────

    [Fact]
    public void InvalidWebhookKey_Throws()
    {
        var act = () => WebhookValidator.Validate("my-secret", "wrong-value");
        act.Should().Throw<UnauthorizedAccessException>();
    }

    // ── 3. NoWebhookSecret_AlwaysPasses ──────────────────────────────────────

    [Fact]
    public void NoWebhookSecret_AlwaysPasses()
    {
        // No secret configured → any (or missing) header is accepted
        var act1 = () => WebhookValidator.Validate(null, null);
        var act2 = () => WebhookValidator.Validate(string.Empty, "anything");
        var act3 = () => WebhookValidator.Validate(null, "random-value");

        act1.Should().NotThrow();
        act2.Should().NotThrow();
        act3.Should().NotThrow();
    }

    // ── 4. EventMapping_DeliveredString_MapsCorrectly ─────────────────────────

    [Fact]
    public void EventMapping_DeliveredString_MapsCorrectly()
    {
        WebhookValidator.MapEventType("delivered").Should().Be(EmailEventType.Delivered);
    }

    // ── 5. EventMapping_UnsubscribeString_MapsCorrectly ──────────────────────

    [Fact]
    public void EventMapping_UnsubscribeString_MapsCorrectly()
    {
        WebhookValidator.MapEventType("unsubscribed").Should().Be(EmailEventType.Unsubscribed);
        WebhookValidator.MapEventType("unsubscribe").Should().Be(EmailEventType.Unsubscribed);
    }

    // ── Additional coverage ───────────────────────────────────────────────────

    [Fact]
    public void EventMapping_AllKnownStrings_MapCorrectly()
    {
        WebhookValidator.MapEventType("sent").Should().Be(EmailEventType.Sent);
        WebhookValidator.MapEventType("opened").Should().Be(EmailEventType.Opened);
        WebhookValidator.MapEventType("clicked").Should().Be(EmailEventType.Clicked);
        WebhookValidator.MapEventType("click").Should().Be(EmailEventType.Clicked);
        WebhookValidator.MapEventType("softbounce").Should().Be(EmailEventType.SoftBounce);
        WebhookValidator.MapEventType("hardbounce").Should().Be(EmailEventType.HardBounce);
        WebhookValidator.MapEventType("spam").Should().Be(EmailEventType.Spam);
        WebhookValidator.MapEventType("blocked").Should().Be(EmailEventType.Blocked);
        WebhookValidator.MapEventType("deferred").Should().Be(EmailEventType.Deferred);
        WebhookValidator.MapEventType("reply").Should().Be(EmailEventType.Reply);
        WebhookValidator.MapEventType("unknown_xyz").Should().Be(EmailEventType.Queued); // fallback
    }

    [Fact]
    public void MissingHeaderWithSecret_Throws()
    {
        var act = () => WebhookValidator.Validate("my-secret", null);
        act.Should().Throw<UnauthorizedAccessException>()
           .WithMessage("*Missing*");
    }
}
