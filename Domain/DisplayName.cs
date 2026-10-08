using System.Globalization;

namespace my_project.Domain;

/// <summary>
/// Rules for participant display names (FR-015, INV-3, EC-9, EC-10).
/// </summary>
public static class DisplayNameRules
{
    public const int MaxLength = 50;

    /// <summary>
    /// Trims the name and reports whether it is acceptable. The length limit counts
    /// user-perceived characters (grapheme clusters), so an emoji or a combined
    /// accented character counts as one, not as two or four (EC-10).
    /// </summary>
    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = (raw ?? string.Empty).Trim();
        if (normalized.Length == 0) return false;

        var graphemes = new StringInfo(normalized).LengthInTextElements;
        return graphemes <= MaxLength;
    }

    /// <summary>
    /// The form used to compare names for uniqueness within a trip: trimmed and
    /// case-insensitive, so "  sam  " collides with "Sam" (INV-3).
    /// </summary>
    public static string ToComparisonKey(string displayName) =>
        displayName.Trim().ToUpperInvariant();
}
