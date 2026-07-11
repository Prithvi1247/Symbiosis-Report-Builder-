using BetaSnapReporting.Models;

namespace BetaSnapReporting.ViewModels.Report;

public class ApplicantMasterViewModel
{
    public string PageTitle { get; set; } = "Applicant Master Portal";
    public bool FileExists { get; set; }
    public int TotalApplicantsCount { get; set; }
    public List<string> ColumnHeaders { get; set; } = new();
    public List<Dictionary<string, object>> SampleRecords { get; set; } = new();
    public ReportMetadata MetadataSummary { get; set; } = new();

    // Sprint 4 Pipeline Variables
    public FilterRequest ActiveFilter { get; set; } = new();
    public int FilteredResultsCount { get; set; }
    public bool IsFiltered { get; set; }

    // Server-side pagination (applied AFTER filtering)
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalPages { get; set; } = 1;
}