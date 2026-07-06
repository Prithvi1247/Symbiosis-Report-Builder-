namespace BetaSnapReporting.Models;

public class FilterCondition
{
    public string ColumnName { get; set; } = string.Empty;
    public string Operator { get; set; } = "Contains"; // e.g., Equals, Contains, Between, >, etc.
    public string Value { get; set; } = string.Empty;
    public string Value2 { get; set; } = string.Empty;  // Dedicated slot for "Between" ranges
    public bool IncludeBlankValues { get; set; }
}