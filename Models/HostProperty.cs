using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Staybnb.Enums;

namespace Staybnb.Models;

public class HostProperty
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Range(1, 1000000, ErrorMessage = "Price per night must be greater than zero.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerNight { get; set; }

    [Range(0, 100000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal CleaningFee { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(100)]
    public string Country { get; set; } = "South Africa";

    public PropertyType PropertyType { get; set; } = PropertyType.Apartment;

    [Range(1, 50)]
    public int MaxGuests { get; set; } = 1;

    [Range(0, 50)]
    public int Bedrooms { get; set; }

    [Range(1, 50)]
    public int Beds { get; set; } = 1;

    [Range(1, 50)]
    public int Bathrooms { get; set; } = 1;

    /// <summary>Set by an Admin when the host application is approved.</summary>
    public bool IsApproved { get; set; }

    /// <summary>Toggled by the Host. A listing is public only if IsApproved AND IsActive.</summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Owner (the applicant, who becomes the Host on approval)
    [Required]
    public string HostId { get; set; } = string.Empty;
    public ApplicationUser Host { get; set; } = null!;

    // Navigation properties
    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

    // One-to-One: each property has one check-in process
    public CheckInProcess? CheckInProcess { get; set; }
}