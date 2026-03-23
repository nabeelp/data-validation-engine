namespace DataValidationEngine.Core.Models;

public class DatabaseInitializationResult
{
    public bool DatabaseCreated { get; set; }
    public int TablesCreated { get; set; }
    public int SampleRulesInserted { get; set; }
    public int SampleRulesSkipped { get; set; }
    public int SampleRulesTotal { get; set; }
}