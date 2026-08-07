using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ViDev.Api.Data.Entities;

/// <summary>
/// Tracks code generation jobs.
/// Maps to TRD §3.3: generation_jobs (id, template_id, status, output_url, compiled, created_at)
/// </summary>
public class GenerationJob
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TemplateId { get; set; }

    [ForeignKey(nameof(TemplateId))]
    public Template Template { get; set; } = null!;

    /// <summary>
    /// Job status: queued → compiling → success / failed.
    /// </summary>
    [Required, MaxLength(32)]
    public string Status { get; set; } = "queued";

    /// <summary>
    /// URL to the generated ZIP file (only set on success).
    /// </summary>
    [MaxLength(1024)]
    public string? OutputUrl { get; set; }

    /// <summary>
    /// Whether the generated project compiled successfully.
    /// </summary>
    public bool Compiled { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
