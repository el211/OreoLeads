using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Enums;

namespace OreoLeads.Infrastructure.Ai.Providers;

/// <summary>Generic OpenAI-compatible provider — same API as OpenAI but with a configurable base URL.</summary>
internal sealed class GenericOpenAiProvider : OpenAiProvider
{
    public override string ProviderName => "Generic OpenAI-Compatible";
    public override AiProviderType ProviderType => AiProviderType.GenericOpenAI;

    public GenericOpenAiProvider(IAiConfigurationService configService, HttpClient http)
        : base(configService, http)
    {
    }
}
