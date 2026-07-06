using BetaSnapReporting.Models;

namespace BetaSnapReporting.Interfaces;

public interface IMetadataService
{
    ReportMetadata GenerateMetadata(IEnumerable<Dictionary<string, object>> dataset, string reportName);
}