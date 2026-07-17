using FluentAssertions;
using OreoLeads.Infrastructure.Configuration;

namespace OreoLeads.Tests.Production;

public class ConfigurationOptionsTests
{
    [Fact]
    public void DatabaseOptions_DefaultValues_AreReasonable()
    {
        var opts = new DatabaseOptions();
        opts.CommandTimeoutSeconds.Should().Be(30);
        opts.MaxRetryCount.Should().Be(3);
        opts.EnableDetailedErrors.Should().BeFalse();
        opts.EnableSensitiveDataLogging.Should().BeFalse();
    }

    [Fact]
    public void JwtOptions_DefaultExpiry_IsPositive()
    {
        var opts = new JwtOptions();
        opts.ExpiryMinutes.Should().BePositive();
        opts.RefreshTokenExpiryDays.Should().BePositive();
    }

    [Fact]
    public void RateLimitOptions_DefaultPermitLimit_IsPositive()
    {
        var opts = new RateLimitOptions();
        opts.PermitLimit.Should().BePositive();
        opts.WindowSeconds.Should().BePositive();
        opts.QueueLimit.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ObservabilityOptions_DefaultServiceName_IsOreoLeads()
    {
        var opts = new ObservabilityOptions();
        opts.ServiceName.Should().Be("OreoLeads");
        opts.EnableTracing.Should().BeTrue();
        opts.EnableMetrics.Should().BeTrue();
    }
}
