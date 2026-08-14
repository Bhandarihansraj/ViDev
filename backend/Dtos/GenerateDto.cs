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
