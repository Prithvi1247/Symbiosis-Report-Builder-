// ReportMetaData.cd
namespace BetaSnapReporting.Models;

public class ReportMetadata
{
    public string ReportName { get; set; } = "Dataset Report";
    public int TotalColumnsCount => Columns.Count;
    public int TotalRecordsCount { get; set; }
    public List<ColumnMetadata> Columns { get; set; } = new();
}