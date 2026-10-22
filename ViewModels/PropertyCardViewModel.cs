using Staybnb.Enums;

namespace Staybnb.ViewModels;

public class PropertyCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public PropertyType PropertyType { get; set; }
    public int MaxGuests { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public string RatingText => ReviewCount == 0 ? "New" : $"{AverageRating:0.0}";
}