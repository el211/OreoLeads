namespace OreoLeads.Application.Features.LeadNotes.DTOs;

public class LeadNoteDto
{
    public Guid Id { get; init; }
    public Guid LeadId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public Guid? AuthorId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public class CreateLeadNoteDto
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string AuthorName { get; init; } = "Système";
}

public class UpdateLeadNoteDto
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
}
