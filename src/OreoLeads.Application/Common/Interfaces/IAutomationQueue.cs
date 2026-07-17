using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities.Automation;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAutomationQueue
{
    Task<AutomationQueueItem> EnqueueAsync(EnqueueAutomationDto dto, CancellationToken ct = default);
    Task<AutomationQueueItem?> DequeueAsync(string workerId, CancellationToken ct = default);
    Task CompleteAsync(Guid itemId, CancellationToken ct = default);
    Task FailAsync(Guid itemId, string error, CancellationToken ct = default);
    Task<List<AutomationQueueItem>> GetPendingItemsAsync(int limit = 100, CancellationToken ct = default);
    Task<int> GetQueueDepthAsync(CancellationToken ct = default);
    Task MoveToDeadLetterAsync(Guid itemId, CancellationToken ct = default);
}
