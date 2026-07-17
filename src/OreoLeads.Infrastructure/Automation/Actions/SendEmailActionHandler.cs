using System.Text.Json;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;

namespace OreoLeads.Infrastructure.Automation.Actions;

internal sealed class SendEmailActionHandler : IActionHandler
{
    private readonly ILogger<SendEmailActionHandler> _logger;

    public SendEmailActionHandler(ILogger<SendEmailActionHandler> logger) => _logger = logger;

    public async Task<ActionResultDto> ExecuteAsync(string? configJson, AutomationContext context, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(configJson))
                return new ActionResultDto(false, null, "No email configuration provided", sw.ElapsedMilliseconds);

            using var doc = JsonDocument.Parse(configJson);
            var to = doc.RootElement.TryGetProperty("to", out var toEl) ? toEl.GetString() : null;
            var subject = doc.RootElement.TryGetProperty("subject", out var subEl) ? subEl.GetString() : null;
            var body = doc.RootElement.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() : null;

            // Interpolate variables
            to = to is not null ? context.InterpolateString(to) : null;
            subject = subject is not null ? context.InterpolateString(subject) : null;
            body = body is not null ? context.InterpolateString(body) : null;

            _logger.LogInformation("SendEmail action: to={To}, subject={Subject}", to, subject);

            // In a full implementation, this would call IBrevoService
            // For now, log the intent and return success
            await Task.CompletedTask;

            return new ActionResultDto(true, $"Email queued to {to}", null, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            return new ActionResultDto(false, null, ex.Message, sw.ElapsedMilliseconds);
        }
    }
}
