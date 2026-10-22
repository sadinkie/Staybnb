using Staybnb.Enums;

namespace Staybnb.Services;

public static class BadgeStyle
{
    public static string For(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Approved => "status-approved",
        ApplicationStatus.Rejected => "status-rejected",
        _ => "status-pending"
    };

    public static string For(BookingStatus status) => status switch
    {
        BookingStatus.Approved => "status-approved",
        BookingStatus.Rejected or BookingStatus.Cancelled => "status-rejected",
        BookingStatus.CheckedIn => "status-info",
        BookingStatus.Completed => "status-neutral",
        _ => "status-pending"
    };

    public static string For(ActivityType type) => type switch
    {
        ActivityType.RoleChange => "status-role",
        ActivityType.Registration or ActivityType.CheckIn
            or ActivityType.BookingStatusChange or ActivityType.PropertyChange => "status-info",
        ActivityType.HostApplication => "status-pending",
        _ => "status-neutral"
    };
}