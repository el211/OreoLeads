using FluentAssertions;
using OreoLeads.Infrastructure.Security;

namespace OreoLeads.Tests.Security;

public class SsrfGuardTests
{
    [Theory]
    [InlineData("http://localhost/api")]
    [InlineData("http://127.0.0.1/")]
    [InlineData("http://127.0.0.1:8080/secret")]
    [InlineData("http://10.0.0.1/admin")]
    [InlineData("http://10.255.255.255/")]
    [InlineData("http://172.16.0.1/")]
    [InlineData("http://172.31.255.255/")]
    [InlineData("http://192.168.1.1/")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]  // AWS metadata
    [InlineData("http://169.254.0.1/")]
    [InlineData("http://100.64.0.1/")]
    public async Task ValidateAsync_Throws_ForBlockedIpLiterals(string url)
    {
        var act = async () => await SsrfGuard.ValidateAsync(url);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData("ftp://example.com/file")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ldap://internal/")]
    [InlineData("gopher://evil.com/")]
    public async Task ValidateAsync_Throws_ForInvalidSchemes(string url)
    {
        var act = async () => await SsrfGuard.ValidateAsync(url);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*scheme*");
    }

    [Fact]
    public async Task ValidateAsync_Throws_ForInvalidUrl()
    {
        var act = async () => await SsrfGuard.ValidateAsync("not-a-url");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid URL*");
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/path")]
    [InlineData("https://www.google.com")]
    public async Task ValidateAsync_DoesNotThrow_ForPublicUrls(string url)
    {
        // These URLs resolve to public IPs and should pass validation.
        // If DNS fails in the test environment, the exception message won't be about SSRF.
        try
        {
            var act = async () => await SsrfGuard.ValidateAsync(url);
            await act.Should().NotThrowAsync<InvalidOperationException>(
                because: $"{url} resolves to a public IP");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("DNS"))
        {
            // DNS unavailable in test environment — skip
        }
    }
}
