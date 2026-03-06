using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using DataValidationEngine.Core.Interfaces;
using DataValidationEngine.Core.Models;
using Microsoft.Extensions.Logging;

namespace DataValidationEngine.Core.Services;

public class FileParser : IFileParser
{
    private readonly ILogger<FileParser> _logger;

    public FileParser(ILogger<FileParser> logger)
    {
        _logger = logger;
    }

    public ParsedFile Parse(Stream fileStream, string fileName, int headerRowCount, int footerRowCount)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        _logger.LogInformation("Parsing file — FileName={FileName}, Extension={Extension}", fileName, extension);

        var allRows = extension switch
        {
            ".csv" => ParseCsv(fileStream),
            ".xlsx" => ParseXlsx(fileStream),
            _ => throw new InvalidOperationException($"Unsupported file type: {extension}")
        };

        var totalRows = allRows.Count;
        _logger.LogInformation("Parsed {TotalRows} rows from {FileName}", totalRows, fileName);

        var headerRows = allRows.Take(headerRowCount).ToList();
        var footerRows = totalRows > headerRowCount
            ? allRows.Skip(Math.Max(headerRowCount, totalRows - footerRowCount)).ToList()
            : [];
        var dataStartIndex = headerRowCount;
        var dataEndIndex = totalRows - footerRowCount;
        var dataRows = dataEndIndex > dataStartIndex
            ? allRows.GetRange(dataStartIndex, dataEndIndex - dataStartIndex)
            : [];

        var columnHeaders = headerRows.Count > 0 ? headerRows[0] : [];

        return new ParsedFile
        {
            FileType = extension.TrimStart('.').ToUpperInvariant(),
            HeaderRows = headerRows,
            DataRows = dataRows,
            FooterRows = footerRows,
            TotalRowCount = totalRows,
            ColumnHeaders = columnHeaders
        };
    }

    private List<string[]> ParseCsv(Stream stream)
    {
        var rows = new List<string[]>();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false
        });

        while (csv.Read())
        {
            var record = new string[csv.Parser.Count];
            for (var i = 0; i < csv.Parser.Count; i++)
                record[i] = csv.GetField(i) ?? string.Empty;
            rows.Add(record);
        }

        return rows;
    }

    private List<string[]> ParseXlsx(Stream stream)
    {
        var rows = new List<string[]>();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var rangeUsed = worksheet.RangeUsed();

        if (rangeUsed == null)
            return rows;

        var rowCount = rangeUsed.RowCount();
        var colCount = rangeUsed.ColumnCount();

        for (var r = 1; r <= rowCount; r++)
        {
            var record = new string[colCount];
            for (var c = 1; c <= colCount; c++)
                record[c - 1] = worksheet.Cell(r, c).GetFormattedString();
            rows.Add(record);
        }

        return rows;
    }
}
