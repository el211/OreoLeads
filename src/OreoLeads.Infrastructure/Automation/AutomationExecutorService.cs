using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Entities.Automation;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Automation.Actions;

namespace OreoLeads.Infrastructure.Automation;

internal sealed class AutomationExecutorService : IAutomationExecutor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutomationExecutorService> _logger;

    public AutomationExecutorService(
        IServiceProvider serviceProvider,
        ILogger<AutomationExecutorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<ActionResultDto> ExecuteActionAsync(AutomationAction action, AutomationContext context, CancellationToken ct = default)
    {
        _logger.LogDebug("Executing action {ActionName} ({ActionType})", action.Name, action.Type);

        IActionHandler handler = action.Type switch
        {
            ActionType.SendEmail => _serviceProvider.GetRequiredService<SendEmailActionHandler>(),
            ActionType.ChangeStatus => _serviceProvider.GetRequiredService<ChangeStatusActionHandler>(),
            ActionType.AddTag => _serviceProvider.GetRequiredService<AddTagActionHandler>(),
            ActionType.RemoveTag => _serviceProvider.GetRequiredService<RemoveTagActionHandler>(),
            ActionType.CreateFollowUp => _serviceProvider.GetRequiredService<CreateFollowUpActionHandler>(),
            ActionType.CreateNote => _serviceProvider.GetRequiredService<CreateNoteActionHandler>(),
            ActionType.HttpRequest or ActionType.WebhookPost or ActionType.WebhookGet
                => _serviceProvider.GetRequiredService<HttpRequestActionHandler>(),
            ActionType.Wait => _serviceProvider.GetRequiredService<WaitActionHandler>(),
            ActionType.SetVariable or ActionType.UpdateVariable
                => _serviceProvider.GetRequiredService<SetVariableActionHandler>(),
            ActionType.ExecuteWorkflow => _serviceProvider.GetRequiredService<ExecuteWorkflowActionHandler>(),
            _ => throw new NotSupportedException($"Action type {action.Type} is not supported")
        };

        return await handler.ExecuteAsync(action.ConfigJson, context, ct);
    }
}
