using System.ComponentModel.DataAnnotations;

namespace ViDev.Api.Dtos;

public sealed class ValidateWiringRequest
{
    [Required]
    public string AstJson { get; set; } = string.Empty;
}
