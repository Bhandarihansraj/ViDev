using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ViDev.Api.Data.Entities;

/// <summary>
/// A registered user who can author and fork templates.
/// Maps to TRD §3.3: users (id, username, auth_provider_id, created_at)
/// </summary>
public class User
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(128)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// External auth provider ID (e.g. from ASP.NET Identity or Clerk).
    /// </summary>
    [MaxLength(256)]
    public string? AuthProviderId { get; set; }

    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Template> Templates { get; set; } = new List<Template>();
}
