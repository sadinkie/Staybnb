using Staybnb.Enums;

namespace Staybnb.ViewModels;

public class PropertyGalleryViewModel
{
    public List<PropertyCardViewModel> Properties { get; set; } = new();

    // Current filters
    public string? City { get; set; }
    public PropertyType? Type { get; set; }

    // Options for the filter controls
    public List<string> Cities { get; set; } = new();
    public List<PropertyType> AvailableTypes { get; set; } = new();

    public bool HasFilters => !string.IsNullOrWhiteSpace(City) || Type.HasValue;
}