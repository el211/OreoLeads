namespace OreoLeads.Application.Common.Interfaces;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        bool succeeded = true,
        string? errorMessage = null,
        CancellationToken ct = default);
}
