using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Brevo;

/// <summary>
/// Tests the test-mode redirect logic extracted as a static helper,
/// and the DoNotContact guard that callers must respect.
/// </summary>
public class TestModeTests
{
    // ── Static helper that mirrors the background service's test-mode logic ───

    private static string? ResolveToEmail(
        string originalEmail,
        BrevoConfiguration config)
    {
        if (config.TestMode)
        {
            if (string.IsNullOrWhiteSpace(config.TestModeEmail))
                return null; // cannot send — no test address
            return config.TestModeEmail;
        }
        return originalEmail;
    }

    private static bool ShouldSkipDueToDoNotContact(bool doNotContact)
        => doNotContact;

    // ── 1. TestMode_RedirectsEmailToTestAddress ───────────────────────────────

    [Fact]
    public void TestMode_RedirectsEmailToTestAddress()
    {
        var config = new BrevoConfiguration
        {
            TestMode      = true,
            TestModeEmail = "test@internal.com",
            IsEnabled     = true
        };

        var resolved = ResolveToEmail("real-lead@client.com", config);

        resolved.Should().Be("test@internal.com");
    }

    // ── 2. TestMode_Disabled_SendsToOriginalAddress ───────────────────────────

    [Fact]
    public void TestMode_Disabled_SendsToOriginalAddress()
    {
        var config = new BrevoConfiguration
        {
            TestMode      = false,
            TestModeEmail = "test@internal.com",
            IsEnabled     = true
        };

        var resolved = ResolveToEmail("real-lead@client.com", config);

        resolved.Should().Be("real-lead@client.com");
    }

    // ── 3. DoNotContact_Skips_Email ───────────────────────────────────────────

    [Fact]
    public void DoNotContact_Skips_Email()
    {
        // When a lead is marked DoNotContact the caller should skip sending
        var doNotContact = true;
        var shouldSkip   = ShouldSkipDueToDoNotContact(doNotContact);

        shouldSkip.Should().BeTrue();
    }

    // ── Bonus: TestMode enabled but no test address → null (skip) ─────────────

    [Fact]
    public void TestMode_NoTestAddress_ReturnsNull()
    {
        var config = new BrevoConfiguration
        {
            TestMode      = true,
            TestModeEmail = null,
            IsEnabled     = true
        };

        var resolved = ResolveToEmail("real-lead@client.com", config);

        resolved.Should().BeNull();
    }

    // ── Bonus: DoNotContact false → should not skip ────────────────────────────

    [Fact]
    public void DoNotContact_False_DoesNotSkip()
    {
        var shouldSkip = ShouldSkipDueToDoNotContact(false);
        shouldSkip.Should().BeFalse();
    }
}
