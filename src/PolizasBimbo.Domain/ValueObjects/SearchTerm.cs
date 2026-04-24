using System.Globalization;
using System.Text;

namespace PolizasBimbo.Domain.ValueObjects;

public sealed record SearchTerm
{
    public const int MinimumLength = 5;

    public string Raw { get; }
    public string Normalized { get; }

    private SearchTerm(string raw, string normalized)
    {
        Raw = raw;
        Normalized = normalized;
    }

    public static SearchTerm Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El término de búsqueda no puede estar vacío.", nameof(value));

        var trimmed = value.Trim();
        if (trimmed.Length < MinimumLength)
            throw new ArgumentException($"El término de búsqueda debe tener al menos {MinimumLength} caracteres.", nameof(value));

        return new SearchTerm(trimmed, Normalize(trimmed));
    }

    public string ToFullTextQuery()
    {
        var tokens = Normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => $"\"{t}*\"");
        return string.Join(" AND ", tokens);
    }

    private static string Normalize(string input)
    {
        var decomposed = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}
