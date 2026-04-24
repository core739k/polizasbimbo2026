using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using PolizasBimbo.Application.Abstractions;
using PolizasBimbo.Domain.Entities;

namespace PolizasBimbo.Infrastructure.Padron;

public sealed class CsvPadronLoader : IPadronLoader
{
    public IEnumerable<Policy> Parse(Stream csvStream)
    {
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            Delimiter = ",",
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
            BadDataFound = null,
            MissingFieldFound = null
        };

        using var reader = new StreamReader(csvStream, System.Text.Encoding.UTF8);
        using var csv = new CsvReader(reader, cfg);

        while (csv.Read())
        {
            if (csv.Parser.Record is null || csv.Parser.Record.Length < 3) continue;
            var numCol = csv.GetField<int>(0);
            var fullName = csv.GetField(1) ?? string.Empty;
            var fileName = csv.GetField(2) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fileName)) continue;
            yield return Policy.Create(0, numCol, fullName, fileName);
        }
    }
}
