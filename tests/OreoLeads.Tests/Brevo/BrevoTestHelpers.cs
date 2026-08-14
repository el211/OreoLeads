using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Brevo.DTOs;

namespace OreoLeads.Tests.Brevo;

/// <summary>Shared stub for IBrevoService used across Brevo tests that don't need HTTP.</summary>
internal sealed class StubBrevoService : IBrevoService
{
    public Task<BrevoTestResultDto> TestConnectionAsync(string apiKey, CancellationToken ct = default)
        => Task.FromResult(new BrevoTestResultDto(true, "OK", null, null));

    public Task<string> SendEmailAsync(EmailSendRequest request, CancellationToken ct = default)
        => Task.FromResult("stub-message-id");

    public Task<string> SendSmsAsync(SmsSendRequest request, CancellationToken ct = default)
        => Task.FromResult("stub-sms-id");

    public Task SyncContactAsync(ContactSyncRequest request, CancellationToken ct = default)
        => Task.CompletedTask;
}
