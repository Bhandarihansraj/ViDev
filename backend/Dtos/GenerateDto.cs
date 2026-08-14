using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ViDev.Api.Dtos;

public class GenerateRequest
{
    [Required]
    public string AstJson { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(128)]
    public string ProjectName { get; set; } = "GeneratedProject";
}

public sealed record CompileResultDto(
    bool Success,
    string Output,
    string Errors,
    long ElapsedMs,
    bool TimedOut,
    List<string> GeneratedFiles
);
