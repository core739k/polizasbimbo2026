namespace PolizasBimbo.Domain.ValueObjects;

public sealed record Phone
{
    public const int RequiredLength = 10;
    public string Value { get; }

    private Phone(string value) => Value = value;

    public static Phone Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Teléfono vacío.", nameof(value));

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != RequiredLength)
            throw new ArgumentException($"El teléfono debe tener {RequiredLength} dígitos.", nameof(value));

        return new Phone(digits);
    }
}
