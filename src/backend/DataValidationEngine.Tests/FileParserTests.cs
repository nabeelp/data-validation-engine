using System.Text;
using ClosedXML.Excel;
using DataValidationEngine.Core.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DataValidationEngine.Tests;

public class FileParserTests
{
    private readonly FileParser _parser;

    public FileParserTests()
    {
        var logger = Substitute.For<ILogger<FileParser>>();
        _parser = new FileParser(logger);
    }

    [Fact]
    public void Parse_Csv_SplitsHeaderDataFooter()
    {
        var csv = "Date,Account,Amount\n2026-01-01,ACC001,100\n2026-01-02,ACC002,200\nTOTAL,,300\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = _parser.Parse(stream, "test.csv", headerRowCount: 1, footerRowCount: 1);

        Assert.Equal("CSV", result.FileType);
        Assert.Single(result.HeaderRows);
        Assert.Equal(2, result.DataRows.Count);
        Assert.Single(result.FooterRows);
        Assert.Equal(4, result.TotalRowCount);
        Assert.Equal(["Date", "Account", "Amount"], result.ColumnHeaders);
    }

    [Fact]
    public void Parse_Csv_SingleRow_OnlyHeader()
    {
        var csv = "Date,Account,Amount\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = _parser.Parse(stream, "test.csv", headerRowCount: 1, footerRowCount: 1);

        Assert.Single(result.HeaderRows);
        Assert.Empty(result.DataRows);
    }

    [Fact]
    public void Parse_Xlsx_SplitsHeaderDataFooter()
    {
        using var stream = CreateTestXlsx(
            ["Date", "Account", "Amount"],
            [["2026-01-01", "ACC001", "100"], ["2026-01-02", "ACC002", "200"]],
            ["TOTAL", "", "300"]);

        var result = _parser.Parse(stream, "test.xlsx", headerRowCount: 1, footerRowCount: 1);

        Assert.Equal("XLSX", result.FileType);
        Assert.Single(result.HeaderRows);
        Assert.Equal(2, result.DataRows.Count);
        Assert.Single(result.FooterRows);
        Assert.Equal(4, result.TotalRowCount);
    }

    [Fact]
    public void Parse_UnsupportedType_Throws()
    {
        using var stream = new MemoryStream();
        Assert.Throws<InvalidOperationException>(() =>
            _parser.Parse(stream, "test.xls", headerRowCount: 1, footerRowCount: 1));
    }

    [Fact]
    public void Parse_Csv_EmptyFile_ReturnsEmptyParsedFile()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));

        var result = _parser.Parse(stream, "empty.csv", headerRowCount: 1, footerRowCount: 1);

        Assert.Equal(0, result.TotalRowCount);
        Assert.Empty(result.HeaderRows);
        Assert.Empty(result.DataRows);
        Assert.Empty(result.FooterRows);
    }

    private static MemoryStream CreateTestXlsx(string[] headers, string[][] dataRows, string[] footer)
    {
        var ms = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var ws = workbook.Worksheets.Add("Sheet1");
            var row = 1;

            for (var c = 0; c < headers.Length; c++)
                ws.Cell(row, c + 1).Value = headers[c];
            row++;

            foreach (var dataRow in dataRows)
            {
                for (var c = 0; c < dataRow.Length; c++)
                    ws.Cell(row, c + 1).Value = dataRow[c];
                row++;
            }

            for (var c = 0; c < footer.Length; c++)
                ws.Cell(row, c + 1).Value = footer[c];

            workbook.SaveAs(ms);
        }

        ms.Position = 0;
        return ms;
    }
}
