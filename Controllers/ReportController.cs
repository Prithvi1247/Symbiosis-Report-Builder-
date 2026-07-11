using System.Text;
using BetaSnapReporting.Interfaces;
using BetaSnapReporting.Models;
using BetaSnapReporting.ViewModels.Report;
using Microsoft.AspNetCore.Mvc;
using MiniExcelLibs;

namespace BetaSnapReporting.Controllers;

public class ReportController : Controller
{
    private readonly IExcelService _excelService;
    private readonly IMetadataService _metadataService;
    private readonly IFilterService _filterService;

    public ReportController(IExcelService excelService, IMetadataService metadataService, IFilterService filterService)
    {
        _excelService = excelService;
        _metadataService = metadataService;
        _filterService = filterService;
    }

    public IActionResult Applicants(FilterRequest request, string submitAction, int? removeIndex)
    {
        var viewModel = new ApplicantMasterViewModel
        {
            FileExists = _excelService.IsWorkbookAvailable()
        };

        if (!viewModel.FileExists)
            return View(viewModel);

        var allApplicants = _excelService.GetApplicants().ToList();
        viewModel.TotalApplicantsCount = allApplicants.Count;
        viewModel.MetadataSummary = _metadataService.GenerateMetadata(allApplicants, "Applicants");

        if (request.Conditions == null)
        {
            request.Conditions = new List<FilterCondition>();
        }

        // Server-Driven State Pipeline Rules (Streamlit Emulation Architecture)
        switch (submitAction)
        {
            case "AddRow":
                request.Conditions.Add(new FilterCondition { ColumnName = "", Operator = "", Value = "" });
                ModelState.Clear(); // Force Razor to completely rebuild elements from the updated Model state
                break;

            case "DeleteRow":
                if (removeIndex.HasValue && removeIndex.Value >= 0 && removeIndex.Value < request.Conditions.Count)
                {
                    request.Conditions.RemoveAt(removeIndex.Value);
                    ModelState.Clear(); // Force Razor to drop deleted DOM indexes entirely
                }
                break;

            case "Reset":
                request = new FilterRequest { Conditions = new List<FilterCondition> { new FilterCondition() } };
                ModelState.Clear();
                break;
        }

        // Guarantee that at least one filter criteria workspace container remains visible
        if (!request.Conditions.Any())
        {
            request.Conditions.Add(new FilterCondition());
        }

        viewModel.ActiveFilter = request;

        // Execute analytical filter processing exclusively when Apply Filters is triggered
        bool performFiltering = (submitAction == "Filter");
        var filteredList = performFiltering
            ? _filterService.ApplyFilter(allApplicants, viewModel.MetadataSummary, request).ToList()
            : allApplicants;

        viewModel.FilteredResultsCount = filteredList.Count;
        viewModel.IsFiltered = performFiltering;
        viewModel.SampleRecords = filteredList.Take(50).ToList();
        viewModel.ColumnHeaders = allApplicants.Any() ? allApplicants.First().Keys.ToList() : new List<string>();

        return View(viewModel);
    }

    public IActionResult ExportCsv(FilterRequest request)
    {
        var (headers, rows) = GetFilteredExportData(request);
        var csv = BuildCsv(headers, rows);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var fileName = $"Applicant_Report_{DateTime.Now:yyyy-MM-dd_HHmm}.csv";
        return File(bytes, "text/csv", fileName);
    }

    public IActionResult ExportExcel(FilterRequest request)
    {
        var (headers, rows) = GetFilteredExportData(request);

        // Reorder each row's dictionary to match the display column order before writing
        var orderedRows = rows.Select(r =>
        {
            var ordered = new Dictionary<string, object>();
            foreach (var h in headers)
            {
                ordered[h] = r.TryGetValue(h, out var v) ? v : "";
            }
            return ordered;
        }).ToList();

        using var stream = new MemoryStream();
        MiniExcel.SaveAs(stream, orderedRows);
        var fileName = $"Applicant_Report_{DateTime.Now:yyyy-MM-dd_HHmm}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // Reuses the existing filter pipeline exactly as the grid does — no second filtering engine.
    private (List<string> Headers, List<Dictionary<string, object>> Rows) GetFilteredExportData(FilterRequest request)
    {
        var allApplicants = _excelService.GetApplicants().ToList();
        var metadata = _metadataService.GenerateMetadata(allApplicants, "Applicants");

        request ??= new FilterRequest();
        request.Conditions ??= new List<FilterCondition>();

        var filtered = _filterService.ApplyFilter(allApplicants, metadata, request).ToList();
        var headers = allApplicants.Any() ? allApplicants.First().Keys.ToList() : new List<string>();

        return (headers, filtered);
    }

    private string BuildCsv(List<string> headers, List<Dictionary<string, object>> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));

        foreach (var row in rows)
        {
            var line = headers.Select(h => EscapeCsvField(row.TryGetValue(h, out var v) ? v?.ToString() ?? "" : ""));
            sb.AppendLine(string.Join(",", line));
        }

        return sb.ToString();
    }

    private string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
        return field;
    }
}