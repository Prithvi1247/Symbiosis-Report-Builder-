namespace BetaSnapReporting.Interfaces;

public interface IExcelService
{
    IEnumerable<Dictionary<string, object>> GetApplicants();
    IEnumerable<Dictionary<string, object>> GetAppliedProgrammes();
    IEnumerable<Dictionary<string, object>> GetProgrammePayments();
    bool IsWorkbookAvailable();
}