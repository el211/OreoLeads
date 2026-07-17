using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Infrastructure.Brevo;

namespace OreoLeads.Tests.Brevo;

public class BrevoServiceTests
{
    private const string FakeApiKey = "xkeysib-test-key";

    // ── 1. TestConnection_WithValidKey_ReturnsSuccess ─────────────────────────

    [Fact]
    public async Task TestConnection_WithValidKey_ReturnsSuccess()
    {
        var response = new
        {
            firstName = "John",
            lastName  = "Doe",
            email     = "john@example.com",
            plan      = new[] { new { type = "free", creditsType = "sendLimit", credits = 300 } }
        };

        var svc = BuildService(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        var result = await svc.TestConnectionAsync(FakeApiKey);

        result.Success.Should().BeTrue();
        result.AccountName.Should().Be("John Doe");
        result.PlanName.Should().Be("free");
    }

    // ── 2. TestConnection_WithInvalidKey_ReturnsFailed ────────────────────────

    [Fact]
    public async Task TestConnection_WithInvalidKey_ReturnsFailed()
    {
        var svc = BuildService(HttpStatusCode.Unauthorized, "{\"code\":\"unauthorized\",\"message\":\"Key not found\"}");

        var result = await svc.TestConnectionAsync("bad-key");

        result.Success.Should().BeFalse();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }

    // ── 3. SendEmail_CallsBrevoApi_ReturnsMessageId ───────────────────────────

    [Fact]
    public async Task SendEmail_CallsBrevoApi_ReturnsMessageId()
    {
        var responseBody = JsonSerializer.Serialize(new { messageId = "<abc123@brevo>" });
        var svc = BuildService(HttpStatusCode.Created, responseBody);

        var request = new EmailSendRequest(
            ApiKey:      FakeApiKey,
            SenderName:  "Sender",
            SenderEmail: "sender@example.com",
            ReplyTo:     null,
            ToEmail:     "lead@example.com",
            ToName:      "Lead Corp",
            Subject:     "Hello!",
            HtmlBody:    "<p>Hi</p>",
            LeadId:      Guid.NewGuid(),
            Tags:        null
        );

        var messageId = await svc.SendEmailAsync(request);

        messageId.Should().Be("<abc123@brevo>");
    }

    // ── 4. SendEmail_On429_RetriesAndSucceeds ─────────────────────────────────

    [Fact]
    public async Task SendEmail_On429_RetriesAndSucceeds()
    {
        var calls = 0;
        var handler = new DelegatingHandlerStub(req =>
        {
            calls++;
            if (calls == 1)
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { messageId = "<retry-success>" }),
                    Encoding.UTF8, "application/json")
            };
        });

        var svc = BuildServiceWithHandler(handler);

        var request = BuildSendRequest();
        var messageId = await svc.SendEmailAsync(request);

        calls.Should().Be(2);
        messageId.Should().Be("<retry-success>");
    }

    // ── 5. SendEmail_On500_RetriesWithBackoff ─────────────────────────────────

    [Fact]
    public async Task SendEmail_On500_RetriesWithBackoff()
    {
        var calls = 0;
        var handler = new DelegatingHandlerStub(req =>
        {
            calls++;
            if (calls < 3)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { messageId = "<after-500>" }),
                    Encoding.UTF8, "application/json")
            };
        });

        var svc = BuildServiceWithHandler(handler);
        var messageId = await svc.SendEmailAsync(BuildSendRequest());

        calls.Should().Be(3);
        messageId.Should().Be("<after-500>");
    }

    // ── 6. SendEmail_AfterMaxRetries_Throws ───────────────────────────────────

    [Fact]
    public async Task SendEmail_AfterMaxRetries_Throws()
    {
        var handler = new DelegatingHandlerStub(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("{\"message\":\"Service unavailable\"}")
            });

        var svc = BuildServiceWithHandler(handler);

        var act = () => svc.SendEmailAsync(BuildSendRequest());
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BrevoService BuildService(HttpStatusCode status, string body)
    {
        var handler = new DelegatingHandlerStub(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
        return BuildServiceWithHandler(handler);
    }

    private static BrevoService BuildServiceWithHandler(DelegatingHandlerStub handler)
    {
        var http = new HttpClient(handler);
        return new BrevoService(http, NullLogger<BrevoService>.Instance);
    }

    private static EmailSendRequest BuildSendRequest() => new(
        ApiKey:      FakeApiKey,
        SenderName:  "Sender",
        SenderEmail: "sender@example.com",
        ReplyTo:     null,
        ToEmail:     "lead@example.com",
        ToName:      "Lead Corp",
        Subject:     "Test",
        HtmlBody:    "<p>test</p>",
        LeadId:      null,
        Tags:        null
    );
}

// ── Stub handler ──────────────────────────────────────────────────────────────

internal sealed class DelegatingHandlerStub : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public DelegatingHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> handler)
        => _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(_handler(request));
}
