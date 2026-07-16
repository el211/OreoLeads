using Microsoft.AspNetCore.Mvc;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Ai.DTOs;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiConfigurationService _configService;
    private readonly IEnumerable<IAiProvider> _providers;

    public AiController(IAiConfigurationService configService, IEnumerable<IAiProvider> providers)
    {
        _configService = configService;
        _providers     = providers;
    }

    /// <summary>Retourne les informations de tous les providers disponibles.</summary>
    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var infos = new[]
        {
            new AiProviderInfoDto(
                "Claude (Anthropic)", AiProviderType.Claude,
                RequiresApiKey: true, RequiresBaseUrl: false,
                DefaultModels: ["claude-opus-4-6", "claude-sonnet-4-6", "claude-haiku-4-5-20251001"],
                "API Anthropic — modèles Claude les plus avancés."),

            new AiProviderInfoDto(
                "OpenAI", AiProviderType.OpenAI,
                RequiresApiKey: true, RequiresBaseUrl: false,
                DefaultModels: ["gpt-4o", "gpt-4o-mini", "gpt-4-turbo"],
                "API OpenAI — GPT-4 et modèles ChatGPT."),

            new AiProviderInfoDto(
                "Ollama (local)", AiProviderType.Ollama,
                RequiresApiKey: false, RequiresBaseUrl: true,
                DefaultModels: ["llama3.2", "mistral", "qwen2.5", "phi4"],
                "Inférence locale — aucun coût, données privées."),

            new AiProviderInfoDto(
                "Generic OpenAI-Compatible", AiProviderType.GenericOpenAI,
                RequiresApiKey: true, RequiresBaseUrl: true,
                DefaultModels: [],
                "Compatible avec tout endpoint OpenAI (Together, Groq, Mistral AI…).")
        };

        return Ok(infos);
    }

    /// <summary>Retourne la configuration AI actuelle (sans la clé en clair).</summary>
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig()
    {
        var cfg = await _configService.GetCurrentAsync();
        if (cfg is null)
            return Ok(new AiConfigurationDto
            {
                ProviderType   = AiProviderType.Claude,
                HasApiKey      = false,
                Temperature    = 0.7f,
                MaxTokens      = 1000,
                TimeoutSeconds = 30,
                IsEnabled      = false
            });

        return Ok(new AiConfigurationDto
        {
            Id             = cfg.Id,
            ProviderType   = cfg.ProviderType,
            HasApiKey      = !string.IsNullOrWhiteSpace(cfg.EncryptedApiKey),
            Model          = cfg.Model,
            Temperature    = cfg.Temperature,
            MaxTokens      = cfg.MaxTokens,
            TimeoutSeconds = cfg.TimeoutSeconds,
            IsEnabled      = cfg.IsEnabled,
            BaseUrl        = cfg.BaseUrl,
            UpdatedAt      = cfg.UpdatedAt
        });
    }

    /// <summary>Sauvegarde la configuration AI.</summary>
    [HttpPut("config")]
    public async Task<IActionResult> UpdateConfig([FromBody] UpdateAiConfigurationDto dto)
    {
        var cfg = await _configService.SaveAsync(dto);
        return Ok(new AiConfigurationDto
        {
            Id             = cfg.Id,
            ProviderType   = cfg.ProviderType,
            HasApiKey      = !string.IsNullOrWhiteSpace(cfg.EncryptedApiKey),
            Model          = cfg.Model,
            Temperature    = cfg.Temperature,
            MaxTokens      = cfg.MaxTokens,
            TimeoutSeconds = cfg.TimeoutSeconds,
            IsEnabled      = cfg.IsEnabled,
            BaseUrl        = cfg.BaseUrl,
            UpdatedAt      = cfg.UpdatedAt
        });
    }

    /// <summary>Teste la connexion avec le provider AI configuré.</summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection(CancellationToken ct)
    {
        var cfg = await _configService.GetCurrentAsync();
        if (cfg is null || !cfg.IsEnabled)
            return Ok(new AiTestResultDto(false, "L'IA n'est pas configurée ou est désactivée.", 0, null));

        var provider = _providers.FirstOrDefault(p => p.ProviderType == cfg.ProviderType);
        if (provider is null)
            return Ok(new AiTestResultDto(false, $"Provider '{cfg.ProviderType}' introuvable.", 0, null));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var ok = await provider.TestConnectionAsync(ct);
        sw.Stop();

        return Ok(ok
            ? new AiTestResultDto(true, "Connexion réussie.", (int)sw.ElapsedMilliseconds, cfg.Model)
            : new AiTestResultDto(false, "Échec de la connexion. Vérifiez la clé API et le modèle.", (int)sw.ElapsedMilliseconds, null));
    }

    /// <summary>Statistiques d'utilisation.</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromServices] IEmailDraftRepository repo)
    {
        var all = await repo.GetAllAsync(1, 9999);
        var items = all.Items.ToList();
        var stats = new AiStatsDto
        {
            TotalGenerations    = all.TotalCount,
            TotalTokens         = items.Sum(e => e.TotalTokens),
            AverageGenerationMs = items.Count > 0 ? items.Average(e => e.GenerationMs) : 0,
            GenerationsByProvider = items
                .GroupBy(e => e.ProviderUsed)
                .ToDictionary(g => g.Key, g => g.Count())
        };
        return Ok(stats);
    }
}
