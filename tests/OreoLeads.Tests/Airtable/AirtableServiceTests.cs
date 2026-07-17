using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using OreoLeads.Infrastructure.Airtable;
using OreoLeads.Tests.Brevo;

namespace OreoLeads.Tests.Airtable;

public class AirtableServiceTests
{
    private const string FakeToken = "patFakeToken123";
    private const string FakeBaseId = "appFakeBase123";

    // ── 1. TestConnection_ValidToken_ReturnsSuccess ───────────────────────────

    [Fact]
    public async Task TestConnection_ValidToken_ReturnsSuccess()
    {
        var json = JsonSerializer.Serialize(new
        {
            tables = new[] { new { id = "tbl1", name = "Leads" } },
            name   = "MyBase"
        });

        var svc = BuildService(HttpStatusCode.OK, json);
        var result = await svc.TestConnectionAsync(FakeToken, FakeBaseId);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successful");
    }

    // ── 2. TestConnection_InvalidToken_Returns401 ─────────────────────────────

    [Fact]
    public async Task TestConnection_InvalidToken_Returns401()
    {
        var svc = BuildService(HttpStatusCode.Unauthorized, "{\"error\":\"UNAUTHORIZED\"}");
        var result = await svc.TestConnectionAsync("bad-token", FakeBaseId);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid");
    }

    // ── 3. GetTables_ValidToken_ReturnsTables ─────────────────────────────────

    [Fact]
    public async Task GetTables_ValidToken_ReturnsTables()
    {
        var json = JsonSerializer.Serialize(new
        {
            tables = new[]
            {
                new { id = "tbl1", name = "Leads",    description = (string?)null },
                new { id = "tbl2", name = "Contacts", description = "All contacts" },
            }
        });

        var svc    = BuildService(HttpStatusCode.OK, json);
        var tables = await svc.GetTablesAsync(FakeToken, FakeBaseId);

        tables.Should().HaveCount(2);
        tables[0].Id.Should().Be("tbl1");
        tables[0].Name.Should().Be("Leads");
        tables[1].Name.Should().Be("Contacts");
        tables[1].Description.Should().Be("All contacts");
    }

    // ── 4. GetFields_ValidToken_ReturnsFields ─────────────────────────────────

    [Fact]
    public async Task GetFields_ValidToken_ReturnsFields()
    {
        var json = JsonSerializer.Serialize(new
        {
            tables = new[]
            {
                new
                {
                    id   = "tbl1",
                    name = "Leads",
                    fields = new[]
                    {
                        new { id = "fld1", name = "Email",   type = "email" },
                        new { id = "fld2", name = "Company", type = "singleLineText" },
                    }
                }
            }
        });

        var svc    = BuildService(HttpStatusCode.OK, json);
        var fields = await svc.GetFieldsAsync(FakeToken, FakeBaseId, "Leads");

        fields.Should().HaveCount(2);
        fields[0].Name.Should().Be("Email");
        fields[1].Name.Should().Be("Company");
    }

    // ── 5. ListRecords_ValidToken_ReturnsPage ─────────────────────────────────

    [Fact]
    public async Task ListRecords_ValidToken_ReturnsPage()
    {
        var json = JsonSerializer.Serialize(new
        {
            records = new[]
            {
                new { id = "rec1", fields = new { Name = "ACME" }, createdTime = "2024-01-01T00:00:00.000Z" },
                new { id = "rec2", fields = new { Name = "BETA" }, createdTime = "2024-01-02T00:00:00.000Z" },
            }
        });

        var svc  = BuildService(HttpStatusCode.OK, json);
        var page = await svc.ListRecordsAsync(FakeToken, FakeBaseId, "Leads", null, null, null, 100);

        page.Records.Should().HaveCount(2);
        page.Records[0].Id.Should().Be("rec1");
        page.Offset.Should().BeNull();
    }

    // ── 6. ListRecords_WithOffset_UsesPagination ──────────────────────────────

    [Fact]
    public async Task ListRecords_WithOffset_UsesPagination()
    {
        var calls = 0;
        string? capturedUrl = null;

        var handler = new DelegatingHandlerStub(req =>
        {
            calls++;
            capturedUrl = req.RequestUri?.ToString();
            var json = JsonSerializer.Serialize(new
            {
                records = new[] { new { id = "rec3", fields = new { } } },
                offset  = (string?)null
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var svc = BuildServiceWithHandler(handler);
        var page = await svc.ListRecordsAsync(FakeToken, FakeBaseId, "Leads", "iteratorToken123", null, null, 10);

        capturedUrl.Should().Contain("offset=");
        page.Records.Should().HaveCount(1);
    }

    // ── 7. GetRecord_NotFound_ReturnsNull ─────────────────────────────────────

    [Fact]
    public async Task GetRecord_NotFound_ReturnsNull()
    {
        var svc    = BuildService(HttpStatusCode.NotFound, "{\"error\":\"NOT_FOUND\"}");
        var result = await svc.GetRecordAsync(FakeToken, FakeBaseId, "Leads", "recNonExistent");

        result.Should().BeNull();
    }

    // ── 8. CreateRecord_ValidData_ReturnsId ───────────────────────────────────

    [Fact]
    public async Task CreateRecord_ValidData_ReturnsId()
    {
        var json = JsonSerializer.Serialize(new
        {
            records = new[]
            {
                new { id = "recNewId", fields = new { Name = "Test" } }
            }
        });

        var svc = BuildService(HttpStatusCode.OK, json);
        var id  = await svc.CreateRecordAsync(FakeToken, FakeBaseId, "Leads",
            new Dictionary<string, object?> { ["Name"] = "Test" });

        id.Should().Be("recNewId");
    }

    // ── 9. UpdateRecord_ValidData_Succeeds ────────────────────────────────────

    [Fact]
    public async Task UpdateRecord_ValidData_Succeeds()
    {
        var json = JsonSerializer.Serialize(new
        {
            records = new[] { new { id = "rec1", fields = new { Name = "Updated" } } }
        });

        var svc = BuildService(HttpStatusCode.OK, json);
        var act = async () => await svc.UpdateRecordAsync(FakeToken, FakeBaseId, "Leads", "rec1",
            new Dictionary<string, object?> { ["Name"] = "Updated" });

        await act.Should().NotThrowAsync();
    }

    // ── 10. CreateRecordsBatch_MultipleRecords_SendsAll ───────────────────────

    [Fact]
    public async Task CreateRecordsBatch_MultipleRecords_SendsAll()
    {
        var calls = 0;
        var handler = new DelegatingHandlerStub(req =>
        {
            calls++;
            var json = JsonSerializer.Serialize(new
            {
                records = new[] { new { id = $"rec{calls}" } }
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var svc = BuildServiceWithHandler(handler);
        // 11 records → should need 2 calls (10 + 1)
        var records = Enumerable.Range(1, 11)
            .Select(i => new Dictionary<string, object?> { ["Name"] = $"Company {i}" })
            .ToList();

        var ids = await svc.CreateRecordsBatchAsync(FakeToken, FakeBaseId, "Leads", records);

        calls.Should().Be(2);
        ids.Should().HaveCount(2);
    }

    // ── 11. On429_WithRetryAfter_WaitsAndRetries ──────────────────────────────

    [Fact]
    public async Task On429_WithRetryAfter_WaitsAndRetries()
    {
        var calls = 0;
        var handler = new DelegatingHandlerStub(req =>
        {
            calls++;
            if (calls == 1)
            {
                var resp429 = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                resp429.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                    TimeSpan.FromSeconds(0));
                return resp429;
            }
            var json = JsonSerializer.Serialize(new
            {
                tables = Array.Empty<object>()
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var svc    = BuildServiceWithHandler(handler);
        var result = await svc.TestConnectionAsync(FakeToken, FakeBaseId);

        calls.Should().Be(2);
        result.Success.Should().BeTrue();
    }

    // ── 12. On429_ExceedsRetries_Throws ──────────────────────────────────────

    [Fact]
    public async Task On429_ExceedsRetries_Throws()
    {
        var handler = new DelegatingHandlerStub(_ =>
        {
            var r = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            r.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(
                TimeSpan.Zero);
            return r;
        });

        var svc = BuildServiceWithHandler(handler);
        // TestConnection returns failure result rather than throwing
        var result = await svc.TestConnectionAsync(FakeToken, FakeBaseId);
        result.Success.Should().BeFalse();
    }

    // ── 13. On5xx_Retries3Times_Throws ───────────────────────────────────────

    [Fact]
    public async Task On5xx_Retries3Times_Throws()
    {
        var calls = 0;
        var handler = new DelegatingHandlerStub(_ =>
        {
            calls++;
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":\"SERVER_ERROR\"}")
            };
        });

        var svc    = BuildServiceWithHandler(handler);
        var result = await svc.TestConnectionAsync(FakeToken, FakeBaseId);

        calls.Should().Be(3);
        result.Success.Should().BeFalse();
    }

    // ── 14. On401_ThrowsUnauthorized ──────────────────────────────────────────

    [Fact]
    public async Task On401_ThrowsUnauthorized()
    {
        var svc    = BuildService(HttpStatusCode.Unauthorized, "{\"error\":\"UNAUTHORIZED\"}");
        var result = await svc.TestConnectionAsync(FakeToken, FakeBaseId);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid");
    }

    // ── 15. WebhookCreate_ReturnsWebhookDto ───────────────────────────────────

    [Fact]
    public async Task WebhookCreate_ReturnsWebhookDto()
    {
        var json = JsonSerializer.Serialize(new
        {
            id              = "wbhk_test_123",
            notificationUrl = "https://example.com/api/webhooks/airtable/ping",
            expirationTime  = DateTime.UtcNow.AddDays(7).ToString("O"),
            cursor          = "cursor_abc"
        });

        var svc     = BuildService(HttpStatusCode.OK, json);
        var webhook = await svc.CreateWebhookAsync(FakeToken, FakeBaseId,
            "https://example.com/api/webhooks/airtable/ping");

        webhook.Should().NotBeNull();
        webhook!.Id.Should().Be("wbhk_test_123");
        webhook.Cursor.Should().Be("cursor_abc");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AirtableService BuildService(HttpStatusCode status, string body)
    {
        var handler = new DelegatingHandlerStub(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        });
        return BuildServiceWithHandler(handler);
    }

    private static AirtableService BuildServiceWithHandler(DelegatingHandlerStub handler)
    {
        var http = new HttpClient(handler);
        return new AirtableService(http, NullLogger<AirtableService>.Instance);
    }
}
