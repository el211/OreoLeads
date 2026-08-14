using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ChatController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>Retourne les 100 derniers messages (ou les messages après 'since').</summary>
    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] DateTime? since, CancellationToken ct)
    {
        var query = _db.ChatMessages.IgnoreQueryFilters().AsQueryable();

        if (since.HasValue)
            query = query.Where(m => m.CreatedAt > since.Value.ToUniversalTime());
        else
            query = query.OrderByDescending(m => m.CreatedAt).Take(100);

        var messages = await query
            .OrderBy(m => m.CreatedAt)
            .Select(m => new
            {
                id         = m.Id,
                userId     = m.UserId,
                authorName = m.AuthorName,
                content    = m.Content,
                createdAt  = m.CreatedAt,
            })
            .ToListAsync(ct);

        return Ok(messages);
    }

    /// <summary>Envoie un message dans le chat commun.</summary>
    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Length > 2000)
            return BadRequest("Contenu invalide (1-2000 caractères).");

        var userId     = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var authorName = User.FindFirstValue(ClaimTypes.Name) ?? "Inconnu";

        var msg = new ChatMessage
        {
            UserId     = userId,
            AuthorName = authorName,
            Content    = dto.Content.Trim(),
        };

        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            id         = msg.Id,
            userId     = msg.UserId,
            authorName = msg.AuthorName,
            content    = msg.Content,
            createdAt  = msg.CreatedAt,
        });
    }
}

public record SendMessageDto(string Content);
