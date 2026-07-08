// FilterRequest.cs
namespace BetaSnapReporting.Models;

public class FilterRequest
{
    public List<FilterCondition> Conditions { get; set; } = new();
    public string LogicalOperator { get; set; } = "AND"; // AND or OR grouping strategy
}