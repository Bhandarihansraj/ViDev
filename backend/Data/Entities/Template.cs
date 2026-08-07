using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ViDev.Api.Data.Entities;

/// <summary>
/// A saved architecture design (AST + wiring contract).
/// Maps to TRD §3.3: templates (id, author_id, name, tags[], ast_json, version, download_count, created_at)
/// </summary>
public class Template
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Template name in namespace format (e.g. "bhandarihansraj/clean-auth").
    /// </summary>
    [Required, MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1024)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Searchable tags stored as a PostgreSQL text array.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Semantic version string (e.g. "1.0.0").
    /// </summary>
    [MaxLength(32)]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// The complete AST JSON — the canonical representation of the architecture design.
    /// Stored as JSONB in PostgreSQL for efficient querying.
    /// </summary>
    [Required]
    [Column(TypeName = "jsonb")]
    public string AstJson { get; set; } = "{}";

    /// <summary>
    /// The wiring contract JSON — field-level connections between layers.
    /// Stored as JSONB in PostgreSQL.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string WiringJson { get; set; } = "{}";

    public int DownloadCount { get; set; } = 0;

    /// <summary>
    /// Whether this template passed the compile-check gate (SECURITY.md §5).
    /// </summary>
    public bool IsVerified { get; set; } = false;

    // Foreign key
    public Guid AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public User Author { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
