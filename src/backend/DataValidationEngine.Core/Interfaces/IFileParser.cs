using DataValidationEngine.Core.Models;

namespace DataValidationEngine.Core.Interfaces;

public interface IFileParser
{
    ParsedFile Parse(Stream fileStream, string fileName, int headerRowCount, int footerRowCount);
}
