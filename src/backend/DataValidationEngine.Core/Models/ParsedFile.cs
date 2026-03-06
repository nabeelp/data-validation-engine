namespace DataValidationEngine.Core.Models;

public class ParsedFile
{
    public string FileType { get; set; } = string.Empty;
    public List<string[]> HeaderRows { get; set; } = [];
    public List<string[]> DataRows { get; set; } = [];
    public List<string[]> FooterRows { get; set; } = [];
    public int TotalRowCount { get; set; }
    public string[] ColumnHeaders { get; set; } = [];
}
