using Staybnb.Enums;
using Staybnb.Models;

namespace Staybnb.ViewModels;

// ---------- Dashboard ----------
public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int PendingHostApplications { get; set; }
    public int ActiveProperties { get; set; }
    public decimal PlatformRevenue { get; set; }
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int ApprovedHosts { get; set; }

    public List<ActivityLogItem> RecentActivity { get; set; } = new();
    public List<HostRequestListItem> RecentApplications { get; set; } = new();
    public List<RecentBookingItem> RecentBookings { get; set; } = new();
}

public class RecentBookingItem
{
    public string GuestName { get; set; } = string.Empty;
    public string? GuestEmail { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public BookingStatus Status { get; set; }
}

// ---------- Activity log ----------
public class ActivityLogItem
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string Action { get; set; } = string.Empty;
    public ActivityType ActivityType { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ActivityLogsViewModel
{
    public List<ActivityLogItem> Items { get; set; } = new();
    public ActivityType? Type { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}

// ---------- Host requests ----------
public class HostRequestListItem
{
    public int ApplicationId { get; set; }
    public string ApplicantName { get; set; } = string.Empty;
    public string? ApplicantEmail { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public string? ThumbnailUrl { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime AppliedAt { get; set; }

    /// <summary>True when the applicant already had an approved application before this one.</summary>
    public bool IsAdditional { get; set; }
}

public class HostRequestsViewModel
{
    public List<HostRequestListItem> Applications { get; set; } = new();
    public ApplicationStatus? Filter { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int TotalCount => PendingCount + ApprovedCount + RejectedCount;
}

public class HostRequestDetailsViewModel
{
    public int ApplicationId { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AdminNotes { get; set; }

    public string ApplicantName { get; set; } = string.Empty;
    public string? ApplicantEmail { get; set; }
    public DateTime ApplicantJoined { get; set; }
    public bool IsAdditional { get; set; }

    public int PropertyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }
    public decimal PricePerNight { get; set; }
    public decimal CleaningFee { get; set; }
    public int MaxGuests { get; set; }
    public int Bedrooms { get; set; }
    public int Beds { get; set; }
    public int Bathrooms { get; set; }

    public List<string> Images { get; set; } = new();
    public List<Amenity> Amenities { get; set; } = new();
}

// ---------- Users ----------
public class UserListItem
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();

    public bool IsGuest => Roles.Contains("Guest");
}

public class UsersViewModel
{
    public List<UserListItem> Users { get; set; } = new();
    public string? Search { get; set; }
    public string Role { get; set; } = "All";
}