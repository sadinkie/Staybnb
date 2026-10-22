namespace Staybnb.Enums;

public static class PropertyTypeExtensions
{
    /// <summary>Font Awesome icon used for each property type.</summary>
    public static string Icon(this PropertyType type) => type switch
    {
        PropertyType.Apartment => "fa-building",
        PropertyType.House => "fa-house",
        PropertyType.Villa => "fa-hotel",
        PropertyType.Cabin => "fa-tree",
        PropertyType.Cottage => "fa-house-chimney",
        PropertyType.Studio => "fa-couch",
        _ => "fa-key"
    };
}