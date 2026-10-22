using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Staybnb.Enums;
using Staybnb.Models;

namespace Staybnb.ViewModels;

public class BecomeHostViewModel
{
    [Required, StringLength(100, MinimumLength = 5)]
    [Display(Name = "Listing title")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 30, ErrorMessage = "Please describe your place in at least 30 characters.")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Property type")]
    public PropertyType PropertyType { get; set; } = PropertyType.Apartment;

    [Required, StringLength(200)]
    [Display(Name = "Street address")]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = "South Africa";

    [Range(1, 1000000, ErrorMessage = "Price per night must be greater than zero.")]
    [Display(Name = "Price per night (R)")]
    public decimal PricePerNight { get; set; }

    [Range(0, 100000, ErrorMessage = "The cleaning fee cannot be negative.")]
    [Display(Name = "Cleaning fee (R)")]
    public decimal CleaningFee { get; set; }

    [Range(1, 50)]
    [Display(Name = "Maximum guests")]
    public int MaxGuests { get; set; } = 2;

    [Range(0, 50)]
    public int Bedrooms { get; set; } = 1;

    [Range(1, 50)]
    public int Beds { get; set; } = 1;

    [Range(1, 50)]
    public int Bathrooms { get; set; } = 1;

    public List<int> SelectedAmenityIds { get; set; } = new();

    /// <summary>At least one image is mandatory. The rule is enforced in PropertyApplicationService.</summary>
    public List<IFormFile> Images { get; set; } = new();

    /// <summary>Only used to draw the checkboxes, never posted back.</summary>
    [ValidateNever]
    public List<Amenity> AvailableAmenities { get; set; } = new();
}