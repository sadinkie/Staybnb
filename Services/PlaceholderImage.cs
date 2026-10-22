namespace Staybnb.Services;

/// <summary>Draws simple coloured house pictures for the demo listings.</summary>
public static class PlaceholderImage
{
    private static readonly (string Start, string End)[] Palettes =
    {
        ("#ff385c", "#ff9a6c"),
        ("#2193b0", "#6dd5ed"),
        ("#56ab2f", "#a8e063"),
        ("#614385", "#516395"),
        ("#f7971e", "#ffd200"),
        ("#134e5e", "#71b280")
    };

    public static string UrlFor(int number) => $"/img/placeholder/{number}";

    public static string Svg(int number)
    {
        var (c1, c2) = Palettes[Math.Abs(number) % Palettes.Length];

        return
            "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 800 760'>" +
            $"<defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>" +
            $"<stop offset='0' stop-color='{c1}'/><stop offset='1' stop-color='{c2}'/>" +
            "</linearGradient></defs>" +
            "<rect width='800' height='760' fill='url(#g)'/>" +
            "<path d='M400 220 L580 370 H540 V520 H260 V370 H220 Z' fill='white' fill-opacity='.9'/>" +
            "<rect x='365' y='420' width='70' height='100' rx='6' fill='black' fill-opacity='.25'/>" +
            "</svg>";
    }
}