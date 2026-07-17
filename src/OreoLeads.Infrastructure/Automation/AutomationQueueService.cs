using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities.Automation;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Infrastructure.Automation;

/// <summary>
/// Singleton queue service. Uses IServiceScopeFactory to access the DB
/// because DbContext is scoped and cannot be injected into a singleton.
/// </summary>
internal sealed class AutomationQueueService : IAutomationQueue
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AutomationQueueService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task<AutomationQueueItem> EnqueueAsync(EnqueueAutomationDto dto, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var item = new AutomationQueueItem
        {
            WorkflowId = dto.WorkflowId,
            Priority = dto.Priority,
            Payload = dto.Payload,
            Status = QueueItemStatus.Pending,
            ScheduledAt = DateTime.UtcNow,
            OrganizationId = dto.OrganizationId
        };

        db.AutomationQueueItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task<AutomationQueueItem?> DequeueAsync(string workerId, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var item = await db.AutomationQueueItems
            .Where(q => q.Status == QueueItemStatus.Pending && q.ScheduledAt <= DateTime.UtcNow)
            .OrderByDescending(q => q.Priority)
            .ThenBy(q => q.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (item is null) return null;

        item.Status = QueueItemStatus.Running;
        item.LockedBy = workerId;
        item.LockedAt = DateTime.UtcNow;
        item.StartedAt = DateTime.UtcNow;
        item.SetUpdatedAt();

        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task CompleteAsync(Guid itemId, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var item = await db.AutomationQueueItems.FindAsync([itemId], ct);
        if (item is null) return;

        item.Status = QueueItemStatus.Completed;
        item.CompletedAt = DateTime.UtcNow;
        item.SetUpdatedAt();

        await db.SaveChangesAsync(ct);
    }

    public async Task FailAsync(Guid itemId, string error, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var item = await db.AutomationQueueItems.FindAsync([itemId], ct);
        if (item is null) return;

        item.RetryCount++;
        item.ErrorMessage = error;

        if (item.RetryCount >= item.MaxRetries)
        {
            item.Status = QueueItemStatus.DeadLetter;
        }
        else
        {
            item.Status = QueueItemStatus.Retrying;
            item.NextRetryAt = DateTime.UtcNow.Add(
                TimeSpan.FromSeconds(Math.Pow(2, item.RetryCount) * 30));
        }

        item.SetUpdatedAt();
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<AutomationQueueItem>> GetPendingItemsAsync(int limit = 100, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.AutomationQueueItems
            .Where(q => q.Status == QueueItemStatus.Pending)
            .OrderByDescending(q => q.Priority)
            .ThenBy(q => q.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<int> GetQueueDepthAsync(CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.AutomationQueueItems
            .CountAsync(q => q.Status == QueueItemStatus.Pending || q.Status == QueueItemStatus.Retrying, ct);
    }

    public async Task MoveToDeadLetterAsync(Guid itemId, CancellationToken ct = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var item = await db.AutomationQueueItems.FindAsync([itemId], ct);
        if (item is null) return;

        item.Status = QueueItemStatus.DeadLetter;
        item.SetUpdatedAt();

        await db.SaveChangesAsync(ct);
    }
}
