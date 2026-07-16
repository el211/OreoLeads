namespace OreoLeads.Application.Features.Tags.DTOs;

public class TagDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = "#6366f1";
}

public class CreateTagDto
{
    public string Name { get; init; } = string.Empty;
    public string Color { get; init; } = "#6366f1";
}
