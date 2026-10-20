namespace Staybnb.Enums;

public enum PropertyType
{
    Apartment,
    House,
    Villa,
    Cabin,
    Cottage,
    Studio,
    Other
}

public enum ApplicationStatus
{
    Pending,
    Approved,
    Rejected
}

public enum BookingStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled,
    CheckedIn,
    Completed
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public enum CheckInStatus
{
    Pending,        // check-in created, guest has not started
    InProgress,     // guest is working through the steps
    Submitted,      // documents uploaded, waiting for host
    Verified,       // host verified, guest is checked in
    Rejected        // host rejected the documents
}

public enum DocumentStatus
{
    Pending,
    Verified,
    Rejected
}

public enum NotificationType
{
    HostApplication,
    AdditionalPropertyApplication,
    Booking,
    CheckIn,
    Message,
    Property,
    System
}

public enum ActivityType
{
    Login,
    Logout,
    Registration,
    RoleChange,
    HostApplication,
    PropertyChange,
    BookingStatusChange,
    CheckIn,
    System
}