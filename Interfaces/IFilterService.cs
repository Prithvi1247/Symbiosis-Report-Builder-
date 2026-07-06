using BetaSnapReporting.Models;

namespace BetaSnapReporting.Interfaces;

public interface IFilterService
{
    IEnumerable<Dictionary<string, object>> ApplyFilter(
        IEnumerable<Dictionary<string, object>> dataset,
        ReportMetadata metadata,
        FilterRequest request);
}