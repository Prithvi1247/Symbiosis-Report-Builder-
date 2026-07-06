namespace BetaSnapReporting.Utilities;

public static class StringExtensions
{
    public static string SafeTrim(this string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? string.Empty : input.Trim();
    }
}