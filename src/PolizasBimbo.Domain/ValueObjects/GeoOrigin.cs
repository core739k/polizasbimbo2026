namespace PolizasBimbo.Domain.ValueObjects;

public sealed record GeoOrigin
{
    public const string Unknown = "Desconocido";
    public const int MaxFieldLength = 100;

    public string Country { get; }
    public string City { get; }

    private GeoOrigin(string country, string city)
    {
        Country = country;
        City = city;
    }

    public static GeoOrigin Create(string? country, string? city)
        => new(Sanitize(country), Sanitize(city));

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return Unknown;
        var trimmed = value.Trim();
        return trimmed.Length > MaxFieldLength ? trimmed[..MaxFieldLength] : trimmed;
    }
}
