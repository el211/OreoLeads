namespace OreoLeads.Domain.Enums;

public enum SyncDirection
{
    OreoLeadsToAirtable,
    AirtableToOreoLeads,
    Bidirectional
}

public enum AirtableSyncJobStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Conflict,
    Cancelled
}

public enum ConflictStrategy
{
    OreoLeadsWins,
    AirtableWins,
    MostRecentWins,
    ManualResolution
}

public enum AirtableFieldType
{
    SingleLineText,
    MultilineText,
    Email,
    PhoneNumber,
    Url,
    Number,
    Checkbox,
    SingleSelect,
    MultipleSelects,
    Date,
    DateTime
}
