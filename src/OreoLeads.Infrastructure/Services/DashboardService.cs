using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Application.Features.Dashboard.DTOs;
using OreoLeads.Application.Features.Leads.DTOs;
using OreoLeads.Application.Features.Tags.DTOs;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Persistence;
using OreoLeads.Infrastructure.Persistence.Repositories;

namespace OreoLeads.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalLeads = await _context.Leads.CountAsync(ct);
        var newLeads = await _context.Leads.CountAsync(l => l.Status == LeadStatus.New, ct);
        var clients = await _context.Leads.CountAsync(l => l.Status == LeadStatus.Client, ct);
        var emailsSent = await _context.Leads.CountAsync(l => l.Status == LeadStatus.EmailSent || l.Status == LeadStatus.FollowUp1 || l.Status == LeadStatus.FollowUp2, ct);
        var pendingFollowUps = await _context.FollowUps.CountAsync(f => f.Status == FollowUpStatus.Pending, ct);
        var leadsThisMonth = await _context.Leads.CountAsync(l => l.CreatedAt >= startOfMonth, ct);

        var statusDistribution = await _context.Leads
            .GroupBy(l => l.Status)
            .Select(g => new StatusDistributionDto
            {
                Status = g.Key.ToString(),
                StatusLabel = LeadRepository.GetStatusLabel(g.Key),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        var industryDistribution = await _context.Leads
            .Where(l => l.Industry != null && l.Industry != "")
            .GroupBy(l => l.Industry!)
            .Select(g => new IndustryDistributionDto { Industry = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct);

        var cityDistribution = await _context.Leads
            .Where(l => l.City != null && l.City != "")
            .GroupBy(l => l.City!)
            .Select(g => new CityDistributionDto { City = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync(ct);

        var recentLeads = await _context.Leads
            .AsNoTracking()
            .Include(l => l.LeadTags).ThenInclude(lt => lt.Tag)
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new LeadSummaryDto
            {
                Id = l.Id,
                CompanyName = l.CompanyName,
                TradeName = l.TradeName,
                Industry = l.Industry,
                City = l.City,
                Department = l.Department,
                Region = l.Region,
                Email = l.Email,
                Phone = l.Phone,
                Website = l.Website,
                Status = l.Status,
                StatusLabel = LeadRepository.GetStatusLabel(l.Status),
                Priority = l.Priority,
                PriorityLabel = LeadRepository.GetPriorityLabel(l.Priority),
                Score = l.Score,
                Tags = l.LeadTags.Select(lt => new TagDto
                {
                    Id = lt.Tag.Id,
                    Name = lt.Tag.Name,
                    Color = lt.Tag.Color
                }).ToList(),
                CreatedAt = l.CreatedAt,
                UpdatedAt = l.UpdatedAt
            })
            .ToListAsync(ct);

        return new DashboardStatsDto
        {
            TotalLeads = totalLeads,
            NewLeads = newLeads,
            Clients = clients,
            EmailsSent = emailsSent,
            PendingFollowUps = pendingFollowUps,
            LeadsThisMonth = leadsThisMonth,
            StatusDistribution = statusDistribution,
            IndustryDistribution = industryDistribution,
            CityDistribution = cityDistribution,
            RecentLeads = recentLeads
        };
    }
}
