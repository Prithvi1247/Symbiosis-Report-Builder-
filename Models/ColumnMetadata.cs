namespace BetaSnapReporting.Models;

public class ColumnMetadata
{
    public string ColumnName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "Text";          // Text, Numeric, Date, Boolean, Categorical
    public string FilterType { get; set; } = "Contains";     // Contains, Range, DateRange, MultiSelect, ExactMatch
    public string WidgetType { get; set; } = "TextBox";      // TextBox, NumberRange, DatePicker, CheckBoxList
    
    public List<string> UniqueValues { get; set; } = new();
    public int NullCount { get; set; }
    public int BlankCount { get; set; }
    public int DistinctCount { get; set; }
    
    public bool HasMissingValues => (NullCount + BlankCount) > 0;
    public bool IsSearchable { get; set; }
    public bool IsFilterable { get; set; }
    public bool IsVisibleByDefault { get; set; }
}