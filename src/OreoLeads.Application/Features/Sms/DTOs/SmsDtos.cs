using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Features.Sms.DTOs;

public record GenerateSmsRequestDto(
    string? CustomInstructions = null
);

public record GenerateSmsResponseDto(
    string Message,
    int    CharCount,
    string ProviderUsed,
    string ModelUsed,
    int    TotalTokens,
    int    GenerationMs
);

public record SendSmsRequestDto(
    string    ToPhone,
    string    Message,
    DateTime? ScheduledAt = null
);

public record SmsSendJobDto(
    Guid          Id,
    Guid          LeadId,
    SmsSendStatus Status,
    DateTime      ScheduledAt,
    DateTime?     SentAt,
    int           AttemptCount,
    int           MaxAttempts,
    DateTime?     NextAttemptAt,
    string?       ErrorMessage,
    string?       BrevoMessageId,
    string        ToPhone,
    string?       ToName,
    string        Message,
    DateTime      CreatedAt
);
