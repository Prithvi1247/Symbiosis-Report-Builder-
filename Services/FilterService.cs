// FilterService.cs
using BetaSnapReporting.Interfaces;
using BetaSnapReporting.Models;

namespace BetaSnapReporting.Services;

public class FilterService : IFilterService
{
    public IEnumerable<Dictionary<string, object>> ApplyFilter(
        IEnumerable<Dictionary<string, object>> dataset,
        ReportMetadata metadata,
        FilterRequest request)
    {
        var rows = dataset?.ToList() ?? new List<Dictionary<string, object>>();
        if (request == null || request.Conditions == null || !request.Conditions.Any())
        {
            return rows;
        }

        // Build a quick lookup dictionary for column metadata types to avoid nested scans
        var metaLookup = metadata.Columns.ToDictionary(c => c.ColumnName, c => c, StringComparer.OrdinalIgnoreCase);

        return rows.Where(row => EvaluateRow(row, request, metaLookup)).ToList();
    }

   private bool EvaluateRow(Dictionary<string, object> row, FilterRequest request, Dictionary<string, ColumnMetadata> metaLookup)
    {
        bool isAnd = request.LogicalOperator.Equals("AND", StringComparison.OrdinalIgnoreCase);
        bool anyEvaluated = false;

        foreach (var condition in request.Conditions)
        {
            if (string.IsNullOrWhiteSpace(condition.ColumnName)) continue;
            anyEvaluated = true;

            bool isMatch = EvaluateCondition(row, condition, metaLookup);

            if (isAnd && !isMatch) return false;
            if (!isAnd && isMatch) return true;
        }

        return isAnd || !anyEvaluated;
    }

    private bool EvaluateCondition(Dictionary<string, object> row, FilterCondition cond, Dictionary<string, ColumnMetadata> metaLookup)
    {
        row.TryGetValue(cond.ColumnName, out var rawVal);
        
        // Null / Empty string handling logic rule
        bool isCellBlank = rawVal == null || rawVal == DBNull.Value || string.IsNullOrWhiteSpace(rawVal.ToString());
        
        if (isCellBlank)
        {
            return cond.IncludeBlankValues;
        }

        string cellStr = rawVal!.ToString()!.Trim();
        if (!metaLookup.TryGetValue(cond.ColumnName, out var colMeta))
        {
            // Fallback to text parsing strategy if metadata reference is missing
            return EvaluateText(cellStr, cond.Operator, cond.Value);
        }

        return colMeta.DataType switch
        {
            "Numeric" => EvaluateNumeric(cellStr, cond.Operator, cond.Value, cond.Value2),
            "Date" => EvaluateDate(cellStr, cond.Operator, cond.Value, cond.Value2),
            "Boolean" => EvaluateCategorical(cellStr, cond.Operator, cond.Value),   // was EvaluateBoolean
            "Categorical" => EvaluateCategorical(cellStr, cond.Operator, cond.Value),
            _ => EvaluateText(cellStr, cond.Operator, cond.Value)
        };
    }

    private bool EvaluateText(string target, string op, string pattern)
    {
        return op switch
        {
            "Equals" => target.Equals(pattern, StringComparison.OrdinalIgnoreCase),
            "Not Equals" => !target.Equals(pattern, StringComparison.OrdinalIgnoreCase),
            "StartsWith" => target.StartsWith(pattern, StringComparison.OrdinalIgnoreCase),
            "EndsWith" => target.EndsWith(pattern, StringComparison.OrdinalIgnoreCase),
            _ => target.Contains(pattern, StringComparison.OrdinalIgnoreCase) // Contains default
        };
    }

    private bool EvaluateNumeric(string target, string op, string val1, string val2)
    {
        if (!double.TryParse(target, out double cellNum) || !double.TryParse(val1, out double num1)) return false;

        return op switch
        {
            "Equals" => cellNum == num1,
            ">" => cellNum > num1,
            ">=" => cellNum >= num1,
            "<" => cellNum < num1,
            "<=" => cellNum <= num1,
            "Between" => double.TryParse(val2, out double num2) && cellNum >= num1 && cellNum <= num2,
            _ => false
        };
    }

    private bool EvaluateDate(string target, string op, string val1, string val2)
    {
        if (!DateTime.TryParse(target, out DateTime cellDate) || !DateTime.TryParse(val1, out DateTime d1)) return false;

        return op switch
        {
            "Equals" => cellDate.Date == d1.Date,
            "Before" => cellDate < d1,
            "After" => cellDate > d1,
            "Between" => DateTime.TryParse(val2, out DateTime d2) && cellDate >= d1 && cellDate <= d2,
            _ => false
        };
    }

    private bool EvaluateBoolean(string target, string op, string value)
    {
        if (!bool.TryParse(target, out bool cellBool) || !bool.TryParse(value, out bool compareBool))
        {
            // Handle numeric fallback representations (e.g., 1/0)
            cellBool = target == "1" || target.Equals("true", StringComparison.OrdinalIgnoreCase);
            compareBool = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        return cellBool == compareBool;
    }

    private bool EvaluateCategorical(string target, string op, string values)
    {
        if (string.IsNullOrWhiteSpace(values)) return false;
        
        // MultiSelect values are provided comma-delimited
        var selectionList = values.Split(',').Select(v => v.Trim()).ToList();
        return selectionList.Contains(target, StringComparer.OrdinalIgnoreCase);
    }
}