using System.ComponentModel.DataAnnotations;
using Staybnb.Enums;

namespace Staybnb.Models;

public class ActivityLog
{
    public int Id { get; set; }

    // Nullable so system events (e.g. seeding) can be logged without a user
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, StringLength(100)]
    public string Action { get; set; } = string.Empty;

    public ActivityType ActivityType { get; set; }

    [StringLength(500)]
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}