using OreoLeads.Domain.Enums;

namespace OreoLeads.Application.Features.Automation.DTOs;

// Core event / result DTOs
public record TriggerEventDto(
    TriggerType Type,
    Guid? LeadId,
    Guid? OrganizationId,
    Dictionary<string, object?> Data);

public record EnqueueAutomationDto(
    Guid WorkflowId,
    TriggerType TriggerType,
    string? Payload,
    int Priority,
    Guid? OrganizationId);

public record AutomationExecutionResultDto(
    bool Success,
    Guid? ExecutionId,
    string Message,
    List<string> Errors);

public record ValidationResultDto(
    bool IsValid,
    List<string> Errors,
    List<string> Warnings);

public record ActionResultDto(
    bool Success,
    string? Output,
    string? Error,
    long DurationMs);

// CRUD DTOs
public record CreateAutomationWorkflowDto(
    string Name,
    string? Description,
    Guid? FolderId,
    string? TriggerJson,
    string? ActionsJson,
    string? VariablesJson,
    string? Tags,
    int ConcurrencyLimit = 1,
    int TimeoutSeconds = 300);

public record UpdateAutomationWorkflowDto(
    string Name,
    string? Description,
    Guid? FolderId,
    bool IsEnabled,
    string? TriggerJson,
    string? ActionsJson,
    string? VariablesJson,
    string? Tags,
    int ConcurrencyLimit = 1,
    int TimeoutSeconds = 300);

// Summary DTOs
public record WorkflowSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    WorkflowStatus Status,
    bool IsEnabled,
    int ExecutionCount,
    DateTime? LastExecutedAt,
    DateTime CreatedAt);

public record ExecutionSummaryDto(
    Guid Id,
    Guid WorkflowId,
    string WorkflowName,
    TriggerType TriggerType,
    ExecutionStatus Status,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    long? DurationMs,
    int RetryCount);

public record MonitoringStatsDto(
    int TotalWorkflows,
    int ActiveWorkflows,
    int TotalExecutions,
    int SuccessfulExecutions,
    int FailedExecutions,
    double AverageSuccessRate,
    double AverageDurationMs,
    int QueueDepth,
    int ActiveJobs,
    int FailedJobs,
    int DeadLetterCount);
