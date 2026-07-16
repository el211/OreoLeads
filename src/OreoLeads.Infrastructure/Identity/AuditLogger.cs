using System.Text.Json;
using Microsoft.AspNetCore.Http;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Identity;

internal sealed class AuditLogger : IAuditLogger
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { WriteIndented = false };

    public AuditLogger(
        ApplicationDbContext context,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(
        string action,
        string entityName,
        string? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        bool succeeded = true,
        string? errorMessage = null,
        CancellationToken ct = default)
    {
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        var log = new AuditLog
        {
            UserId = _currentUser.UserId,
            UserEmail = _currentUser.UserEmail,
            OrganizationId = _currentUser.OrganizationId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues, JsonOpts) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues, JsonOpts) : null,
            IpAddress = ipAddress,
            Succeeded = succeeded,
            ErrorMessage = errorMessage,
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync(ct);
    }
}
