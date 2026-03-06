namespace DataValidationEngine.Core.Interfaces;

public interface IIngestionService
{
    Task IngestFileAsync(string fileName);
}
