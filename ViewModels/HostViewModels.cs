using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Staybnb.Enums;

namespace Staybnb.ViewModels;

// ---------- Dashboard ----------
public class HostDashboardViewModel
{
    public string FirstName { get; set; } = string.Empty;

    public int TotalProperties { get; set; }
    public int ActiveProperties { get; set; }
    public int PendingProperties { get; set; }

    public int TotalBookings { get; set; }
    public int BookingsThisMonth { get; set; }
    public int PendingBookingRequests { get; set; }

    public decimal TotalEarnings { get; set; }
    public decimal EarningsThisMonth { get; set; }

    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public double OccupancyRate { get; set; }
    public double AcceptanceRate { get; set; }
    public decimal AverageNightlyRate { get; set; }
    public decimal AverageBookingValue { get; set; }

    public List<HostBookingItem> RecentBookings { get; set; } = new();
}

public class HostBookingItem
{
    public string GuestName { get; set; } = string.Empty;
    public string? GuestEmail { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public BookingStatus Status { get; set; }
    public decimal Earnings { get; set; }
}

// ---------- My Properties ----------
public class HostPropertyListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public PropertyType PropertyType { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public int Beds { get; set; }
    public int Bathrooms { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int ImageCount { get; set; }

    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }
    public ApplicationStatus? LatestApplicationStatus { get; set; }
    public string? AdminNotes { get; set; }

    public int BookingCount { get; set; }
    public int ReviewCount { get; set; }
    public double AverageRating { get; set; }

    public bool IsRejected => !IsApproved && LatestApplicationStatus == ApplicationStatus.Rejected;
    public bool IsPending => !IsApproved && !IsRejected;

    public string StatusLabel =>
        IsRejected ? "Rejected" :
        IsPending ? "Pending review" :
        IsActive ? "Active" : "Inactive";

    public string StatusClass =>
        IsRejected ? "status-rejected" :
        IsPending ? "status-pending" :
        IsActive ? "status-approved" : "status-neutral";

    public string RatingText => ReviewCount == 0 ? "No reviews" : $"{AverageRating:0.0} ({ReviewCount})";
}

// ---------- Edit ----------
public class ExistingImageItem
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
}

/// <summary>Same fields as the application form, plus the photos that already exist.</summary>
public class EditPropertyViewModel : BecomeHostViewModel
{
    public int Id { get; set; }

    [ValidateNever]
    public List<ExistingImageItem> ExistingImages { get; set; } = new();

    /// <summary>Ids of existing photos the Host ticked for removal.</summary>
    public List<int> RemoveImageIds { get; set; } = new();
}