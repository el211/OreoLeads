using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Application.Features.Sms.DTOs;

namespace OreoLeads.Infrastructure.Ai;

internal sealed class SmsGeneratorService : ISmsGeneratorService
{
    private readonly IAiConfigurationService    _aiConfig;
    private readonly IEnumerable<IAiProvider>   _providers;
    private readonly IPromptBuilder             _promptBuilder;
    private readonly IBrevoConfigurationService _brevoConfig;

    public SmsGeneratorService(
        IAiConfigurationService     aiConfig,
        IEnumerable<IAiProvider>    providers,
        IPromptBuilder              promptBuilder,
        IBrevoConfigurationService  brevoConfig)
    {
        _aiConfig      = aiConfig;
        _providers     = providers;
        _promptBuilder = promptBuilder;
        _brevoConfig   = brevoConfig;
    }

    public async Task<GenerateSmsResponseDto> GenerateAsync(
        Guid leadId, GenerateSmsRequestDto request, CancellationToken ct = default)
    {
        var config = await _aiConfig.GetCurrentAsync()
                     ?? throw new InvalidOperationException("L'IA n'est pas configurée.");

        if (!config.IsEnabled)
            throw new InvalidOperationException("L'IA est désactivée. Activez-la dans les paramètres.");

        var provider = _providers.FirstOrDefault(p => p.ProviderType == config.ProviderType)
                       ?? throw new InvalidOperationException($"Fournisseur '{config.ProviderType}' non enregistré.");

        // Récupère les coordonnées de contact depuis la config Brevo
        var brevo         = await _brevoConfig.GetCurrentAsync(ct);
        var contactPhone  = brevo?.ContactPhone;
        var contactEmail  = brevo?.SenderEmail;

        var systemPrompt = await _promptBuilder.BuildSmsSystemPromptAsync(ct);
        var userPrompt   = await _promptBuilder.BuildSmsUserPromptAsync(leadId, request, contactPhone, contactEmail, ct);

        // SMS needs few tokens — cap at 200 to keep response tight
        var completion = await provider.CompleteAsync(
            new AiCompletionRequest(systemPrompt, userPrompt, MaxTokens: 200, Temperature: config.Temperature), ct);

        // Clean up the response: strip quotes, trim whitespace, enforce 160-char limit
        var message = completion.Content
            .Trim()
            .Trim('"', '\'')
            .Trim();

        if (message.Length > 160)
            message = message[..160];

        return new GenerateSmsResponseDto(
            Message:      message,
            CharCount:    message.Length,
            ProviderUsed: provider.ProviderName,
            ModelUsed:    completion.Model,
            TotalTokens:  completion.TotalTokens,
            GenerationMs: completion.GenerationMs);
    }
}
