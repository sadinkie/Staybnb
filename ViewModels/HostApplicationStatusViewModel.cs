using Staybnb.Enums;

namespace Staybnb.ViewModels;

public class HostApplicationStatusItem
{
    public int ApplicationId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public string? ThumbnailUrl { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AdminNotes { get; set; }
}

public class HostApplicationStatusViewModel
{
    public List<HostApplicationStatusItem> Applications { get; set; } = new();

    /// <summary>False while an application is still waiting for review.</summary>
    public bool CanApply { get; set; }
}