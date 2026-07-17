using System.Text.RegularExpressions;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Infrastructure.Automation;

internal sealed class AutomationVariableResolver
{
    private static readonly Regex VariablePattern = new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);

    public string Resolve(string template, AutomationContext ctx)
    {
        if (string.IsNullOrEmpty(template)) return template;
        return ctx.InterpolateString(template);
    }
}
