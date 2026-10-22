
using System.ComponentModel.DataAnnotations;

namespace Staybnb.ViewModels;

public class CreateBookingViewModel
{
    public int PropertyId { get; set; }

    public string PropertyTitle { get; set; } = string.Empty;

    public decimal PricePerNight { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal ServiceFee { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckInDate { get; set; } = DateTime.Today.AddDays(1);

    [Required]
    [DataType(DataType.Date)]
    public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(2);

    [Range(1, 50)]
    public int NumberOfGuests { get; set; } = 1;

    public int MaxGuests { get; set; }

    public int Nights =>
        Math.Max(0, (CheckOutDate.Date - CheckInDate.Date).Days);

    public decimal TotalPrice =>
        (PricePerNight * Nights) + CleaningFee + ServiceFee;
}
