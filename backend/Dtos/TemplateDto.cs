using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ViDev.Api.Dtos;

public class CreateTemplateRequest
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();

    [Required]
    public string AstJson { get; set; } = string.Empty;

    public string? WiringJson { get; set; }
}

public class TemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Version { get; set; } = string.Empty;
    public string AstJson { get; set; } = string.Empty;
    public string? WiringJson { get; set; }
    public int DownloadCount { get; set; }
    public bool IsVerified { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class TemplateListResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Version { get; set; } = string.Empty;
    public int DownloadCount { get; set; }
    public bool IsVerified { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
