using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ViDev.Api.Data;
using ViDev.Api.Data.Entities;
using ViDev.Api.Dtos;

namespace ViDev.Api.Services;

public class TemplateService : ITemplateService
{
    private readonly ViDevDbContext _dbContext;

    public TemplateService(ViDevDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TemplateResponse> CreateAsync(CreateTemplateRequest request, Guid authorId)
    {
        var template = new Template
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            Tags = request.Tags ?? new List<string>(),
            AstJson = request.AstJson,
            WiringJson = request.WiringJson ?? "{}",
            AuthorId = authorId,
            Version = "1.0.0",
            IsVerified = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Templates.Add(template);
        await _dbContext.SaveChangesAsync();

        // Get author for response
        var author = await _dbContext.Users.FindAsync(authorId);

        return new TemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Tags = template.Tags,
            Version = template.Version,
            AstJson = template.AstJson,
            WiringJson = template.WiringJson,
            DownloadCount = template.DownloadCount,
            IsVerified = template.IsVerified,
            AuthorId = template.AuthorId,
            AuthorUsername = author?.Username ?? string.Empty,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }

    public async Task<List<TemplateListResponse>> GetAllAsync(string? tagFilter, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 50) pageSize = 50;

        var query = _dbContext.Templates
            .Include(t => t.Author)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(tagFilter))
        {
            query = query.Where(t => t.Tags.Contains(tagFilter));
        }

        var templates = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return templates.Select(t => new TemplateListResponse
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Tags = t.Tags,
            Version = t.Version,
            DownloadCount = t.DownloadCount,
            IsVerified = t.IsVerified,
            AuthorUsername = t.Author?.Username ?? string.Empty,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    public async Task<TemplateResponse?> GetByIdAsync(Guid id)
    {
        var template = await _dbContext.Templates
            .Include(t => t.Author)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null) return null;

        return new TemplateResponse
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Tags = template.Tags,
            Version = template.Version,
            AstJson = template.AstJson,
            WiringJson = template.WiringJson,
            DownloadCount = template.DownloadCount,
            IsVerified = template.IsVerified,
            AuthorId = template.AuthorId,
            AuthorUsername = template.Author?.Username ?? string.Empty,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt
        };
    }
}
