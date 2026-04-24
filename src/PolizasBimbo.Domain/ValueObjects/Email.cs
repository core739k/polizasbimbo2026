using System.Net.Mail;

namespace PolizasBimbo.Domain.ValueObjects;

public sealed record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email vacío.", nameof(value));

        var trimmed = value.Trim().ToLowerInvariant();
        if (!MailAddress.TryCreate(trimmed, out _))
            throw new ArgumentException($"Email inválido: '{value}'.", nameof(value));

        return new Email(trimmed);
    }
}
