using OreoLeads.Domain.Common;

namespace OreoLeads.Domain.Entities;

public class ChatMessage : BaseEntity
{
    public string UserId     { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Content    { get; set; } = string.Empty;
}
