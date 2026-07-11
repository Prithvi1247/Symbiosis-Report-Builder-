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

    private const int PageSize = 50;

    public IActionResult Applicants(FilterRequest request, string submitAction, int? removeIndex, int? pageNumber)
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

        switch (submitAction)
        {
            case "AddRow":
                request.Conditions.Add(new FilterCondition { ColumnName = "", Operator = "", Value = "" });
                ModelState.Clear();
                break;

            case "DeleteRow":
                if (removeIndex.HasValue && removeIndex.Value >= 0 && removeIndex.Value < request.Conditions.Count)
                {
                    request.Conditions.RemoveAt(removeIndex.Value);
                    ModelState.Clear();
                }
                break;

            case "Reset":
                request = new FilterRequest { Conditions = new List<FilterCondition> { new FilterCondition() } };
                ModelState.Clear();
                break;
        }

        if (!request.Conditions.Any())
        {
            request.Conditions.Add(new FilterCondition());
        }

        // Resolve which columns are visible. Metadata-driven default (ColumnMetadata.IsVisibleByDefault)
        // is used only on a genuine first load; any submission of the Visible Columns panel (tracked via
        // the hidden marker field, since an all-unchecked submit sends no VisibleColumns entries at all)
        // takes precedence and is honored exactly as the user configured it.
        if (Request.Query.ContainsKey("VisibleColumnsSubmitted"))
        {
            request.VisibleColumns = request.VisibleColumns ?? new List<string>();
        }
        else
        {
            request.VisibleColumns = viewModel.MetadataSummary.Columns
                .Where(c => c.IsVisibleByDefault)
                .Select(c => c.ColumnName)
                .ToList();

            if (!request.VisibleColumns.Any())
            {
                request.VisibleColumns = viewModel.MetadataSummary.Columns.Select(c => c.ColumnName).ToList();
            }
        }

        viewModel.ActiveFilter = request;

        bool skipFiltering = submitAction == "AddRow" || submitAction == "DeleteRow" || submitAction == "Reset";
        var filteredList = skipFiltering
            ? allApplicants
            : _filterService.ApplyFilter(allApplicants, viewModel.MetadataSummary, request).ToList();

        viewModel.FilteredResultsCount = filteredList.Count;
        viewModel.IsFiltered = !skipFiltering && request.Conditions.Any(c =>
            !string.IsNullOrWhiteSpace(c.ColumnName) &&
            (!string.IsNullOrWhiteSpace(c.Value) || !string.IsNullOrWhiteSpace(c.Value2)));

        bool filterConfigChanged = submitAction == "Filter" || submitAction == "AddRow" || submitAction == "DeleteRow" || submitAction == "Reset";

        int totalPages = (int)Math.Ceiling(filteredList.Count / (double)PageSize);
        if (totalPages < 1) totalPages = 1;

        int currentPage = filterConfigChanged ? 1 : (pageNumber ?? 1);
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages) currentPage = totalPages;

        viewModel.CurrentPage = currentPage;
        viewModel.PageSize = PageSize;
        viewModel.TotalPages = totalPages;

        viewModel.SampleRecords = filteredList
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        var allColumnHeaders = allApplicants.Any() ? allApplicants.First().Keys.ToList() : new List<string>();
        viewModel.ColumnHeaders = allColumnHeaders
            .Where(h => request.VisibleColumns.Contains(h, StringComparer.OrdinalIgnoreCase))
            .ToList();

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

        // Reorder each row's dictionary to match the display column order, with S.No first.
        var orderedRows = new List<Dictionary<string, object>>();
        int serialNo = 1;
        foreach (var r in rows)
        {
            var ordered = new Dictionary<string, object> { ["S.No"] = serialNo };
            foreach (var h in headers)
            {
                ordered[h] = r.TryGetValue(h, out var v) ? v : "";
            }
            orderedRows.Add(ordered);
            serialNo++;
        }

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
        var allHeaders = allApplicants.Any() ? allApplicants.First().Keys.ToList() : new List<string>();

        // Export only the columns currently checked in the Visible Columns panel.
        // If none were submitted (e.g. a direct export call with no selection), export everything.
        List<string> headers = (request.VisibleColumns != null && request.VisibleColumns.Any())
            ? allHeaders.Where(h => request.VisibleColumns.Contains(h, StringComparer.OrdinalIgnoreCase)).ToList()
            : allHeaders;

        return (headers, filtered);
    }

    private string BuildCsv(List<string> headers, List<Dictionary<string, object>> rows)
    {
        var sb = new StringBuilder();
        var csvHeaders = new List<string> { "S.No" };
        csvHeaders.AddRange(headers);
        sb.AppendLine(string.Join(",", csvHeaders.Select(EscapeCsvField)));

        int serialNo = 1;
        foreach (var row in rows)
        {
            var line = new List<string> { serialNo.ToString() };
            line.AddRange(headers.Select(h => EscapeCsvField(row.TryGetValue(h, out var v) ? v?.ToString() ?? "" : "")));
            sb.AppendLine(string.Join(",", line));
            serialNo++;
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