// MetadataService.cs
using System.Text.RegularExpressions;
using BetaSnapReporting.Interfaces;
using BetaSnapReporting.Models;

namespace BetaSnapReporting.Services;

public class MetadataService : IMetadataService
{
    private static readonly HashSet<string> DefaultVisibleColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "StudentID", "ApplicantID", "Applicant Name", "ApplicantName", "Gender", "Category", "City", "State", "Registration Date", "RegistrationDate"
    };

    public ReportMetadata GenerateMetadata(IEnumerable<Dictionary<string, object>> dataset, string reportName)
    {
        var reportMetadata = new ReportMetadata { ReportName = reportName };
        var rows = dataset?.ToList() ?? new List<Dictionary<string, object>>();
        reportMetadata.TotalRecordsCount = rows.Count;

        if (rows.Count == 0) return reportMetadata;

        // Extract all column names from the first record entry shell dynamically
        var columnNames = rows.First().Keys;

        foreach (var colName in columnNames)
        {
            var colMeta = new ColumnMetadata
            {
                ColumnName = colName,
                DisplayName = GenerateFriendlyName(colName),
                IsVisibleByDefault = DefaultVisibleColumns.Contains(colName) || DefaultVisibleColumns.Contains(GenerateFriendlyName(colName))
            };

            var nonNullValues = new List<string>();
            int nullCount = 0;
            int blankCount = 0;

            // Heuristic detection arrays
            bool canBeNumeric = true;
            bool canBeDate = true;
            bool canBeBoolean = true;

            foreach (var row in rows)
            {
                if (!row.TryGetValue(colName, out var rawVal) || rawVal == null || rawVal == DBNull.Value)
                {
                    nullCount++;
                    continue;
                }

                string strVal = rawVal.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(strVal))
                {
                    blankCount++;
                    continue;
                }

                string trimmedVal = strVal.Trim();
                nonNullValues.Add(trimmedVal);

                // Type convergence validations
                if (canBeNumeric && !double.TryParse(trimmedVal, out _)) canBeNumeric = false;
                if (canBeDate && !DateTime.TryParse(trimmedVal, out _)) canBeDate = false;
                if (canBeBoolean && !bool.TryParse(trimmedVal, out _) && trimmedVal != "1" && trimmedVal != "0") canBeBoolean = false;
            }

            colMeta.NullCount = nullCount;
            colMeta.BlankCount = blankCount;

            // Compute cardinality details safely using case-insensitive comparers
            var distinctItems = nonNullValues.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(v => v).ToList();
            colMeta.DistinctCount = distinctItems.Count;

            // 1. Assign Data Type
            if (nonNullValues.Count == 0)
            {
                colMeta.DataType = "Text";
            }
            else if (canBeBoolean)
            {
                colMeta.DataType = "Boolean";
            }
            else if (canBeNumeric)
            {
                // Edge-case check: If a numeric code acts like an identifier, treat it as Text categorical instead
                if (colName.Contains("ID", StringComparison.OrdinalIgnoreCase) && colMeta.DistinctCount > 100)
                    colMeta.DataType = "Text";
                else
                    colMeta.DataType = "Numeric";
            }
            else if (canBeDate)
            {
                colMeta.DataType = "Date";
            }
            else if (colMeta.DistinctCount <= 45 || (colMeta.DistinctCount < (rows.Count * 0.25))) 
            {
                // Heuristic bound: If highly repetitive string vectors exist, classify as Categorical selection arrays
                colMeta.DataType = "Categorical";
            }
            else
            {
                colMeta.DataType = "Text";
            }

            // 2. Assign Filters and Widgets derived completely from dynamic metadata state
            switch (colMeta.DataType)
            {
                case "Boolean":
                    colMeta.FilterType = "ExactMatch";
                    colMeta.WidgetType = "CheckBoxList";
                    colMeta.UniqueValues = distinctItems;
                    colMeta.IsFilterable = true;
                    colMeta.IsSearchable = false;
                    break;

                case "Numeric":
                    colMeta.FilterType = "Range";
                    colMeta.WidgetType = "NumberRange";
                    colMeta.IsFilterable = true;
                    colMeta.IsSearchable = false;
                    break;

                case "Date":
                    colMeta.FilterType = "DateRange";
                    colMeta.WidgetType = "DatePicker";
                    colMeta.IsFilterable = true;
                    colMeta.IsSearchable = false;
                    break;

                case "Categorical":
                    colMeta.FilterType = "MultiSelect";
                    colMeta.WidgetType = "CheckBoxList";
                    colMeta.UniqueValues = distinctItems; // Cleaned, sorted, case-grouped distinct items
                    colMeta.IsFilterable = true;
                    colMeta.IsSearchable = true;
                    break;

                default: // Text fields
                    if (colName.Contains("ID", StringComparison.OrdinalIgnoreCase) && colMeta.DistinctCount == rows.Count)
                    {
                        colMeta.FilterType = "ExactMatch";
                        colMeta.WidgetType = "TextBox";
                        colMeta.IsFilterable = true;
                        colMeta.IsSearchable = true;
                    }
                    else
                    {
                        colMeta.FilterType = "Contains";
                        colMeta.WidgetType = "TextBox";
                        colMeta.IsFilterable = true;
                        colMeta.IsSearchable = true;
                    }
                    break;
            }

            reportMetadata.Columns.Add(colMeta);
        }

        return reportMetadata;
    }

    private string GenerateFriendlyName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName)) return string.Empty;

        // Strip pipe characters or trailing codes if passed from underlying mapping tables
        string name = columnName.Split('|')[0].Split('-')[0].Trim();

        // Standardize snake/camel splits safely without breaking formatting spaces
        name = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        name = name.Replace("_", " ");
        
        // Collapse internal structural whitespace runs
        return Regex.Replace(name, @"\s+", " ").Trim();
    }
}