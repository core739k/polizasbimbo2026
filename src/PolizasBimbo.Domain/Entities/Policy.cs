namespace PolizasBimbo.Domain.Entities;

public sealed class Policy
{
    public int Id { get; }
    public int NumColaborador { get; }
    public string FullName { get; }
    public string FileName { get; }

    private Policy(int id, int numColaborador, string fullName, string fileName)
    {
        Id = id;
        NumColaborador = numColaborador;
        FullName = fullName;
        FileName = fileName;
    }

    public static Policy Create(int id, int numColaborador, string? fullName, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName es obligatorio.", nameof(fileName));
        return new Policy(id, numColaborador, fullName?.Trim() ?? string.Empty, fileName.Trim());
    }
}
