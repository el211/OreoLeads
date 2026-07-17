namespace OreoLeads.Domain.Enums;

public enum TriggerType
{
    LeadCreated, LeadUpdated, LeadDeleted, StatusChanged,
    TagAdded, TagRemoved, LeadAssigned, FollowUpCreated, FollowUpDue,
    FollowUpCompleted, EmailSent, EmailDelivered, EmailOpened, EmailClicked,
    EmailReplied, EmailBounced, EmailFailed, AirtableSync, WebhookReceived,
    DateTime, Cron, Delay, Manual, Api
}

public enum ActionType
{
    SendEmail, CreateFollowUp, ChangeStatus, AddTag, RemoveTag,
    CreateNote, CreateActivity, ExportAirtable, ImportAirtable, HttpRequest,
    WebhookPost, WebhookGet, Wait, SetVariable, UpdateVariable,
    CreateLead, UpdateLead, DeleteLead, ExecuteWorkflow, CancelWorkflow
}

public enum ConditionOperator
{
    Equals, NotEquals, GreaterThan, LessThan, GreaterThanOrEquals,
    LessThanOrEquals, Contains, NotContains, StartsWith, EndsWith, IsNull,
    IsNotNull, In, NotIn
}

public enum LogicOperator
{
    And, Or, Not
}

public enum ExecutionStatus
{
    Pending, Running, Waiting, Completed, Failed, Cancelled, TimedOut, Skipped
}

public enum QueueItemStatus
{
    Pending, Running, Waiting, Retrying, Completed, Failed, Cancelled, TimedOut, Skipped, DeadLetter
}

public enum WorkflowStatus
{
    Draft, Active, Paused, Archived
}

public enum ScheduleInterval
{
    EveryMinute, EveryHour, Daily, Weekly, Monthly, Cron
}
