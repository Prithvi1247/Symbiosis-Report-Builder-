namespace BetaSnapReporting.Models;

public class FilterRequest
{
    public List<FilterCondition> Conditions { get; set; } = new();
    public string LogicalOperator { get; set; } = "AND"; // AND or OR grouping strategy
    public List<string> VisibleColumns { get; set; } = new(); // Columns currently checked "visible" in the grid/export
}