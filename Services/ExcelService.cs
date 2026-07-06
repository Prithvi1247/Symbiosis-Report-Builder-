using BetaSnapReporting.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;

namespace BetaSnapReporting.Services;

public class ExcelService : IExcelService
{
    private readonly string _filePath;
    private readonly ILogger<ExcelService> _logger;
    private readonly object _lock = new();

    private List<Dictionary<string, object>>? _applicantsCache;
    private List<Dictionary<string, object>>? _appliedProgrammesCache;
    private List<Dictionary<string, object>>? _programmePaymentsCache;

    public ExcelService(IHostEnvironment environment, ILogger<ExcelService> logger)
    {
        _filePath = Path.Combine(environment.ContentRootPath, "Data", "Beta_SNAP_Normalized.xlsx");
        _logger = logger;
    }

    public bool IsWorkbookAvailable()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogWarning("Excel workbook source target not found at expected path.");
                return false;
            }

            using (var stream = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return stream.Length > 0;
            }
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Workbook file is inaccessible or locked by another process.");
            return false;
        }
    }

    public IEnumerable<Dictionary<string, object>> GetApplicants()
    {
        return SafeLoadSheet("Applicants", ref _applicantsCache);
    }

    public IEnumerable<Dictionary<string, object>> GetAppliedProgrammes()
    {
        return SafeLoadSheet("Applied_Programmes", ref _appliedProgrammesCache);
    }

    public IEnumerable<Dictionary<string, object>> GetProgrammePayments()
    {
        return SafeLoadSheet("Programme_Payments", ref _programmePaymentsCache);
    }

    private IEnumerable<Dictionary<string, object>> SafeLoadSheet(string sheetName, ref List<Dictionary<string, object>>? cache)
    {
        if (!IsWorkbookAvailable())
        {
            return Enumerable.Empty<Dictionary<string, object>>();
        }

        lock (_lock)
        {
            if (cache != null) return cache;

            try
            {
                var sheetNames = MiniExcel.GetSheetNames(_filePath);
                if (!sheetNames.Contains(sheetName))
                {
                    _logger.LogError("Target sheet '{SheetName}' was not found in the normalized workbook.", sheetName);
                    cache = new List<Dictionary<string, object>>();
                    return cache;
                }

                // FIX: Added useHeaderRow: true to map columns to the workbook's first row names
                var rows = MiniExcel.Query(_filePath, useHeaderRow: true, sheetName: sheetName)
                                    .Cast<IDictionary<string, object>>();
                                    
                cache = rows.Select(r => new Dictionary<string, object>(r!)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical failure parsing spreadsheet sheet content: '{SheetName}'. File may be corrupt.", sheetName);
                cache = new List<Dictionary<string, object>>();
            }

            return cache;
        }
    }
}