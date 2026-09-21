namespace CodeFormatter.Models;

/// <summary>
/// One tab of the settings dialog.
/// </summary>
public sealed record SettingsPage(string Title, SettingDefinition[] Settings)
{
    public int Rows => Settings.Sum(SettingsPages.RowsOf);
}

/// <summary>
/// Splits a formatter's settings into the tabs of the settings dialog.
///
/// A DevToys dialog neither scrolls nor can be given a size: it grows with its content, and what
/// does not fit the window runs out underneath it. So a page holds only as much as fits, and every
/// page gets the same fixed height. A group with more settings than that becomes several tabs.
/// </summary>
public static class SettingsPages
{
    /// <summary>Rows that fit on one page in a DevToys window of ordinary height.</summary>
    public const int MaxRows = 5;

    public const string DefaultTitle = "General";

    /// <summary>A text setting has its input on a line of its own, so it is as tall as two rows.</summary>
    public static int RowsOf(SettingDefinition setting) => setting.Type == SettingType.Text ? 2 : 1;

    public static IReadOnlyList<SettingsPage> Build(IEnumerable<SettingDefinition> settings)
    {
        var pages = new List<SettingsPage>();

        // Groups in the order they first appear; GroupBy keeps that order
        foreach (var group in settings.GroupBy(s => s.Group ?? DefaultTitle))
        {
            var chunks = new List<List<SettingDefinition>> { new() };
            foreach (var setting in group)
            {
                if (chunks[^1].Sum(RowsOf) + RowsOf(setting) > MaxRows)
                    chunks.Add([]);
                chunks[^1].Add(setting);
            }

            for (var i = 0; i < chunks.Count; i++)
                pages.Add(new SettingsPage(i == 0 ? group.Key : $"{group.Key} {i + 1}", chunks[i].ToArray()));
        }

        return pages;
    }
}
