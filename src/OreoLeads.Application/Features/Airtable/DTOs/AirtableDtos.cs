using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Features.Airtable.DTOs;

public record AirtableConfigurationDto(
    Guid             Id,
    string           ConnectionName,
    bool             HasAccessToken,
    string           BaseId,
    string           TableIdOrName,
    bool             IsEnabled,
    SyncDirection    SyncDirection,
    ConflictStrategy ConflictStrategy,
    DateTime?        LastSyncAt,
    bool             HasWebhook,
    DateTime?        WebhookExpiresAt,
    DateTime         CreatedAt,
    DateTime?        UpdatedAt
);

public record UpdateAirtableConfigurationDto(
    string?          AccessToken,
    string           ConnectionName,
    string           BaseId,
    string           TableIdOrName,
    bool             IsEnabled,
    SyncDirection    SyncDirection,
    ConflictStrategy ConflictStrategy
);

public record AirtableTestResultDto(
    bool    Success,
    string  Message,
    string? WorkspaceName,
    string? BaseName
);

public record AirtableTableDto(
    string  Id,
    string  Name,
    string? Description
);

public record AirtableFieldDto(
    string            Id,
    string            Name,
    AirtableFieldType Type
);

public record AirtableRecordDto(
    string                      Id,
    Dictionary<string, object?> Fields,
    DateTime?                   CreatedTime,
    DateTime?                   ModifiedTime
);

public record AirtableRecordsPageDto(
    List<AirtableRecordDto> Records,
    string?                 Offset
);

public record AirtableWebhookDto(
    string    Id,
    string    NotificationUrl,
    DateTime? ExpirationTime,
    string?   Cursor
);

public record AirtableWebhookChangesDto(
    string                         Cursor,
    bool                           MightHaveMore,
    List<AirtableWebhookChangeDto> Changes
);

public record AirtableWebhookChangeDto(
    string                       TableId,
    string?                      RecordId,
    string                       ChangeType,  // "create", "update", "delete"
    Dictionary<string, object?>? ChangedFields
);

public record AirtableFieldMappingDto(
    Guid              Id,
    Guid              AirtableConfigurationId,
    string            OreoLeadsField,
    string            AirtableFieldName,
    AirtableFieldType AirtableFieldType,
    SyncDirection     Direction,
    bool              IsRequired,
    string?           DefaultValue,
    string?           Transformation,
    int               SortOrder
);

public record SaveAirtableFieldMappingDto(
    string            OreoLeadsField,
    string            AirtableFieldName,
    AirtableFieldType AirtableFieldType,
    SyncDirection     Direction,
    bool              IsRequired,
    string?           DefaultValue,
    string?           Transformation,
    int               SortOrder
);

public record EnqueueAirtableSyncDto(
    Guid          AirtableConfigurationId,
    SyncDirection Direction,
    bool          IsFullSync,
    Guid?         LeadId,
    string?       TriggerReason
);

public record AirtableSyncJobDto(
    Guid                  Id,
    Guid                  AirtableConfigurationId,
    AirtableSyncJobStatus Status,
    SyncDirection         Direction,
    string?               TriggerReason,
    bool                  IsFullSync,
    Guid?                 LeadId,
    int                   TotalRecords,
    int                   ProcessedRecords,
    int                   SuccessRecords,
    int                   FailedRecords,
    int                   ConflictRecords,
    int                   AttemptCount,
    int                   MaxAttempts,
    DateTime?             StartedAt,
    DateTime?             CompletedAt,
    DateTime?             NextAttemptAt,
    string?               ErrorMessage,
    DateTime              CreatedAt
);

public record AirtableSyncLogDto(
    Guid     Id,
    Guid     AirtableSyncJobId,
    Guid?    LeadId,
    string?  AirtableRecordId,
    string   Action,
    string?  Details,
    string?  ErrorMessage,
    bool     Success,
    DateTime OccurredAt
);

public record AirtableRecordLinkDto(
    Guid                   Id,
    Guid                   LeadId,
    string?                LeadName,
    Guid                   AirtableConfigurationId,
    string                 AirtableRecordId,
    DateTime?              LastSyncedAt,
    AirtableSyncJobStatus? ConflictStatus,
    string?                ConflictOreoLeadsData,
    string?                ConflictAirtableData,
    DateTime?              ConflictDetectedAt,
    DateTime?              AirtableModifiedAt
);

public record ConflictResolutionDto(
    string WinnerSource   // "oreoleads" or "airtable"
);

public record AirtableWebhookPayloadDto(
    string  BaseId,
    string  WebhookId,
    long    Timestamp,
    string? Cursor
);

public record AirtableSyncStatsDto(
    int       TotalSyncs,
    int       SuccessfulSyncs,
    int       FailedSyncs,
    int       TotalExported,
    int       TotalImported,
    int       ActiveConflicts,
    DateTime? LastSyncAt
);
