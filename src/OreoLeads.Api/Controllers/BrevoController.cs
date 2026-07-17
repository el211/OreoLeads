using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Brevo.DTOs;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/brevo")]
public class BrevoController : ControllerBase
{
    private readonly IBrevoConfigurationService _configSvc;
    private readonly IEmailStatsService         _statsSvc;

    public BrevoController(IBrevoConfigurationService configSvc, IEmailStatsService statsSvc)
    {
        _configSvc = configSvc;
        _statsSvc  = statsSvc;
    }

    /// <summary>Returns current Brevo configuration (API key is never returned in clear).</summary>
    [HttpGet]
    public async Task<IActionResult> GetConfiguration(CancellationToken ct)
    {
        var config = await _configSvc.GetCurrentAsync(ct);
        if (config is null)
            return Ok(DefaultDto());

        return Ok(ToDto(config));
    }

    /// <summary>Saves (upserts) Brevo configuration.</summary>
    [HttpPut]
    public async Task<IActionResult> UpdateConfiguration([FromBody] UpdateBrevoConfigurationDto dto, CancellationToken ct)
    {
        var config = await _configSvc.SaveAsync(dto, ct);
        return Ok(ToDto(config));
    }

    /// <summary>Tests the connection to Brevo with the stored API key.</summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection(CancellationToken ct)
    {
        var result = await _configSvc.TestConnectionAsync(ct);
        return Ok(result);
    }

    /// <summary>Returns email sending statistics.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] Guid?     organizationId = null,
        [FromQuery] DateTime? from           = null,
        [FromQuery] DateTime? to             = null,
        CancellationToken ct                 = default)
    {
        var stats = await _statsSvc.GetStatsAsync(organizationId, from, to, ct);
        return Ok(stats);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static BrevoConfigurationDto ToDto(BrevoConfiguration c) => new(
        Id:               c.Id,
        HasApiKey:        !string.IsNullOrWhiteSpace(c.EncryptedApiKey),
        SenderName:       c.SenderName,
        SenderEmail:      c.SenderEmail,
        ReplyTo:          c.ReplyTo,
        IsEnabled:        c.IsEnabled,
        TestMode:         c.TestMode,
        TestModeEmail:    c.TestModeEmail,
        DailyLimit:       c.DailyLimit,
        HasWebhookSecret: !string.IsNullOrWhiteSpace(c.WebhookSecret),
        CreatedAt:        c.CreatedAt,
        UpdatedAt:        c.UpdatedAt
    );

    private static BrevoConfigurationDto DefaultDto() => new(
        Id:               Guid.Empty,
        HasApiKey:        false,
        SenderName:       string.Empty,
        SenderEmail:      string.Empty,
        ReplyTo:          null,
        IsEnabled:        false,
        TestMode:         false,
        TestModeEmail:    null,
        DailyLimit:       300,
        HasWebhookSecret: false,
        CreatedAt:        DateTime.UtcNow,
        UpdatedAt:        null
    );
}
