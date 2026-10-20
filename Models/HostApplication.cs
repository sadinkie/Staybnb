using System.ComponentModel.DataAnnotations;
using Staybnb.Enums;

namespace Staybnb.Models;

public class HostApplication
{
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;
    public ApplicationUser ApplicationUser { get; set; } = null!;

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    // Review details (filled in by the Admin)
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedById { get; set; }

    [StringLength(500)]
    public string? AdminNotes { get; set; }
}