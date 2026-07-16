using System.Text.Json;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Ai;

internal sealed class EmailGeneratorService : IEmailGeneratorService
{
    private readonly IAiConfigurationService _aiConfig;
    private readonly IEnumerable<IAiProvider> _providers;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IEmailDraftRepository _draftRepo;

    public EmailGeneratorService(
        IAiConfigurationService aiConfig,
        IEnumerable<IAiProvider> providers,
        IPromptBuilder promptBuilder,
        IEmailDraftRepository draftRepo)
    {
        _aiConfig      = aiConfig;
        _providers     = providers;
        _promptBuilder = promptBuilder;
        _draftRepo     = draftRepo;
    }

    public async Task<GeneratedEmail> GenerateAsync(
        Guid leadId, GenerateEmailRequestDto request, CancellationToken ct = default)
    {
        var config = await _aiConfig.GetCurrentAsync()
                     ?? throw new InvalidOperationException("AI is not configured.");

        if (!config.IsEnabled)
            throw new InvalidOperationException("AI is disabled. Enable it in the settings.");

        var provider = _providers.FirstOrDefault(p => p.ProviderType == config.ProviderType)
                       ?? throw new InvalidOperationException($"Provider '{config.ProviderType}' is not registered.");

        var systemPrompt = await _promptBuilder.BuildEmailSystemPromptAsync(leadId, request, ct);
        var userPrompt   = await _promptBuilder.BuildEmailUserPromptAsync(leadId, request, ct);

        var completion = await provider.CompleteAsync(
            new AiCompletionRequest(systemPrompt, userPrompt, config.MaxTokens, config.Temperature), ct);

        var parsed = ParseResponse(completion.Content);

        var draft = new GeneratedEmail
        {
            LeadId           = leadId,
            Subject          = parsed.Subject,
            Body             = parsed.Body,
            Summary          = parsed.Summary,
            CallToAction     = parsed.CallToAction,
            Status           = EmailStatus.Generated,
            Style            = request.Style,
            Length           = request.Length,
            Type             = request.Type,
            ProviderUsed     = provider.ProviderName,
            ModelUsed        = completion.Model,
            PromptTokens     = completion.PromptTokens,
            CompletionTokens = completion.CompletionTokens,
            TotalTokens      = completion.TotalTokens,
            GenerationMs     = completion.GenerationMs,
            CurrentVersion   = 1
        };

        draft = await _draftRepo.CreateAsync(draft);

        // Save first version
        await _draftRepo.AddVersionAsync(new EmailDraftVersion
        {
            EmailDraftId = draft.Id,
            Version      = 1,
            Subject      = draft.Subject,
            Body         = draft.Body,
            ProviderUsed = draft.ProviderUsed,
            ModelUsed    = draft.ModelUsed,
            GenerationMs = draft.GenerationMs
        });

        return draft;
    }

    public async Task<EmailDraftVersion> RegenerateAsync(Guid draftId, CancellationToken ct = default)
    {
        var draft = await _draftRepo.GetByIdAsync(draftId)
                    ?? throw new InvalidOperationException($"Email draft {draftId} not found.");

        var config = await _aiConfig.GetCurrentAsync()
                     ?? throw new InvalidOperationException("AI is not configured.");

        if (!config.IsEnabled)
            throw new InvalidOperationException("AI is disabled.");

        var provider = _providers.FirstOrDefault(p => p.ProviderType == config.ProviderType)
                       ?? throw new InvalidOperationException($"Provider '{config.ProviderType}' is not registered.");

        var request = new GenerateEmailRequestDto
        {
            Style  = draft.Style,
            Length = draft.Length,
            Type   = draft.Type
        };

        var systemPrompt = await _promptBuilder.BuildEmailSystemPromptAsync(draft.LeadId, request, ct);
        var userPrompt   = await _promptBuilder.BuildEmailUserPromptAsync(draft.LeadId, request, ct);

        var completion = await provider.CompleteAsync(
            new AiCompletionRequest(systemPrompt, userPrompt, config.MaxTokens, config.Temperature), ct);

        var parsed = ParseResponse(completion.Content);

        // Update draft with latest content
        draft.Subject          = parsed.Subject;
        draft.Body             = parsed.Body;
        draft.Summary          = parsed.Summary;
        draft.CallToAction     = parsed.CallToAction;
        draft.Status           = EmailStatus.Generated;  // reset to Generated
        draft.ProviderUsed     = provider.ProviderName;
        draft.ModelUsed        = completion.Model;
        draft.PromptTokens    += completion.PromptTokens;
        draft.CompletionTokens+= completion.CompletionTokens;
        draft.TotalTokens     += completion.TotalTokens;
        draft.GenerationMs     = completion.GenerationMs;
        draft.CurrentVersion  += 1;
        draft.SetUpdatedAt();

        await _draftRepo.UpdateAsync(draft);

        // Save new version
        var version = new EmailDraftVersion
        {
            EmailDraftId = draft.Id,
            Version      = draft.CurrentVersion,
            Subject      = draft.Subject,
            Body         = draft.Body,
            ProviderUsed = draft.ProviderUsed,
            ModelUsed    = draft.ModelUsed,
            GenerationMs = completion.GenerationMs
        };

        return await _draftRepo.AddVersionAsync(version);
    }

    // ── Response parser ───────────────────────────────────────────────────────

    private static ParsedEmailResponse ParseResponse(string content)
    {
        // Try to extract JSON block from the response
        var jsonStart = content.IndexOf('{');
        var jsonEnd   = content.LastIndexOf('}');

        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var jsonStr = content[jsonStart..(jsonEnd + 1)];
            try
            {
                var parsed = JsonSerializer.Deserialize<ParsedEmailResponse>(jsonStr,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Subject))
                    return parsed;
            }
            catch { /* fall through to heuristic parsing */ }
        }

        // Heuristic: look for SUBJECT: / BODY: lines
        return ParseHeuristic(content);
    }

    private static ParsedEmailResponse ParseHeuristic(string content)
    {
        var lines   = content.Split('\n', StringSplitOptions.None);
        string? subj = null;
        var bodyLines = new List<string>();
        bool inBody  = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("SUBJECT:", StringComparison.OrdinalIgnoreCase))
            {
                subj = trimmed[8..].Trim();
            }
            else if (trimmed.StartsWith("BODY:", StringComparison.OrdinalIgnoreCase))
            {
                inBody = true;
                var rest = trimmed[5..].Trim();
                if (!string.IsNullOrEmpty(rest)) bodyLines.Add(rest);
            }
            else if (inBody)
            {
                bodyLines.Add(line);
            }
        }

        return new ParsedEmailResponse
        {
            Subject     = subj ?? "Email commercial — Oreo Studios",
            Body        = bodyLines.Count > 0 ? string.Join("\n", bodyLines).Trim() : content.Trim(),
            Summary     = null,
            CallToAction = null
        };
    }

    private sealed class ParsedEmailResponse
    {
        public string Subject      { get; set; } = string.Empty;
        public string Body         { get; set; } = string.Empty;
        public string? Summary     { get; set; }
        public string? CallToAction { get; set; }
    }
}
