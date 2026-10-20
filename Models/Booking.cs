using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Staybnb.Enums;

namespace Staybnb.Models;

public class Booking
{
    public int Id { get; set; }

    public int PropertyId { get; set; }
    public HostProperty Property { get; set; } = null!;

    [Required]
    public string GuestId { get; set; } = string.Empty;
    public ApplicationUser Guest { get; set; } = null!;

    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }

    [Range(1, 50)]
    public int NumberOfGuests { get; set; } = 1;

    // Price snapshot taken at booking time:
    // TotalPrice = (PricePerNight x Nights) + CleaningFee + ServiceFee
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerNight { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CleaningFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ServiceFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public int Nights => (CheckOutDate.Date - CheckInDate.Date).Days;

    // One-to-One: each booking has one payment and one guest check-in
    public Payment? Payment { get; set; }
    public GuestCheckIn? GuestCheckIn { get; set; }
}