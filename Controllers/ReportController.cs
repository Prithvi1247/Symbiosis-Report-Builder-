using BetaSnapReporting.Interfaces;
using BetaSnapReporting.Models;
using BetaSnapReporting.ViewModels.Report;
using Microsoft.AspNetCore.Mvc;

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
}