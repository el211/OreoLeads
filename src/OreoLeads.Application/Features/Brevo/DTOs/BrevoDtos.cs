using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Features.Brevo.DTOs;

public record BrevoConfigurationDto(
    Guid      Id,
    bool      HasApiKey,
    string    SenderName,
    string    SenderEmail,
    string?   ReplyTo,
    string?   ContactPhone,
    bool      IsEnabled,
    bool      TestMode,
    string?   TestModeEmail,
    int       DailyLimit,
    bool      HasWebhookSecret,
    DateTime  CreatedAt,
    DateTime? UpdatedAt
);

public record UpdateBrevoConfigurationDto(
    string?   ApiKey,
    string    SenderName,
    string    SenderEmail,
    string?   ReplyTo,
    string?   ContactPhone,
    bool      IsEnabled,
    bool      TestMode,
    string?   TestModeEmail,
    int       DailyLimit,
    string?   WebhookSecret
);

public record BrevoTestResultDto(
    bool    Success,
    string  Message,
    string? AccountName,
    string? PlanName
);

public record EmailSendJobDto(
    Guid            Id,
    Guid            GeneratedEmailId,
    Guid            LeadId,
    EmailSendStatus Status,
    DateTime        ScheduledAt,
    DateTime?       SentAt,
    int             AttemptCount,
    int             MaxAttempts,
    DateTime?       NextAttemptAt,
    string?         ErrorMessage,
    string?         BrevoMessageId,
    string          ToEmail,
    string?         ToName,
    string          Subject,
    DateTime        CreatedAt
);

public record EmailStatsDto(
    int    TotalSent,
    int    TotalDelivered,
    int    TotalOpened,
    int    TotalClicked,
    int    TotalBounced,
    int    TotalSpam,
    int    TotalUnsubscribed,
    double OpenRate,
    double ClickRate,
    double BounceRate,
    int    ReplyCount
);

public record QueueEmailRequestDto(
    Guid      GeneratedEmailId,
    DateTime? ScheduledAt
);

public class BrevoWebhookPayloadDto
{
    public string?       Event     { get; set; }
    public string?       Email     { get; set; }
    public string?       MessageId { get; set; }
    public string?       Date      { get; set; }
    public string?       LeadId    { get; set; }
    public List<string>? Tags      { get; set; }
    public string?       Reason    { get; set; }
    public string?       Ip        { get; set; }
}
