using CodeFormatter.Formatters;
using CodeFormatter.Models;

namespace CodeFormatter.Tests;

/// <summary>
/// The settings dialog cannot scroll and cannot be resized, so what goes on it has to fit.
/// These limits come from looking at the real dialog, not from taste.
/// </summary>
public class SettingsPagesTests
{
    public static IEnumerable<object[]> AllLanguages() =>
        LanguageRegistry.All.Select(info => new object[] { info.Language });

    private static SettingDefinition Bool(string key, string? group = null) =>
        new(key, key, SettingType.Boolean, false, Group: group);

    [Fact]
    public void Groups_BecomeTabs_InTheOrderTheyFirstAppear()
    {
        var pages = SettingsPages.Build([Bool("a", "Layout"), Bool("b", "Style"), Bool("c", "Layout")]);

        Assert.Equal(["Layout", "Style"], pages.Select(p => p.Title));
        Assert.Equal(["a", "c"], pages[0].Settings.Select(s => s.Key));
    }

    [Fact]
    public void SettingsWithoutAGroup_ShareTheGeneralTab()
    {
        var pages = SettingsPages.Build([Bool("a"), Bool("b", SettingsPages.DefaultTitle), Bool("c", "Style")]);

        Assert.Equal([SettingsPages.DefaultTitle, "Style"], pages.Select(p => p.Title));
        Assert.Equal(2, pages[0].Settings.Length);
    }

    [Fact]
    public void AGroupTooBigForOnePage_IsSplit_RatherThanOverflowing()
    {
        var pages = SettingsPages.Build(Enumerable.Range(0, 12).Select(i => Bool($"s{i}", "Spacing")));

        Assert.Equal(["Spacing", "Spacing 2", "Spacing 3"], pages.Select(p => p.Title));
        Assert.All(pages, p => Assert.True(p.Rows <= SettingsPages.MaxRows));
        Assert.Equal(12, pages.Sum(p => p.Settings.Length));
    }

    [Fact]
    public void ATextSetting_TakesTwoRows()
    {
        var text = new SettingDefinition("t", "t", SettingType.Text, "", Group: "Advanced");
        var pages = SettingsPages.Build([Bool("a", "Advanced"), Bool("b", "Advanced"), Bool("c", "Advanced"), Bool("d", "Advanced"), text]);

        // 4 + 2 rows do not fit on one page of 5
        Assert.Equal(2, pages.Count);
        Assert.Equal(2, pages[1].Rows);
    }

    [Fact]
    public void Docs_GoOnTheAdvancedTab_AndTakeARowOfIt()
    {
        var advanced = Enumerable.Range(0, 5).Select(i => Bool($"s{i}", SettingsPages.AdvancedTitle));
        var pages = SettingsPages.Build([Bool("a", "Layout"), .. advanced], hasDocs: true);

        // Five settings and the documentation line do not fit on one page
        Assert.Equal(["Layout", "Advanced", "Advanced 2"], pages.Select(p => p.Title));
        Assert.True(pages[1].ShowsDocs);
        Assert.Equal(SettingsPages.MaxRows, pages[1].Rows);
    }

    [Fact]
    public void Docs_GetATabOfTheirOwn_WhenThereIsNoAdvancedTab()
    {
        var pages = SettingsPages.Build([Bool("a")], hasDocs: true);

        Assert.Equal([SettingsPages.DefaultTitle, SettingsPages.AboutTitle], pages.Select(p => p.Title));
        Assert.Empty(pages[1].Settings);
        Assert.True(pages[1].ShowsDocs);
    }

    [Theory]
    [MemberData(nameof(AllLanguages))]
    public void EveryLanguage_FitsTheDialog(Language language)
    {
        var spec = FormatterSpecs.For(language)!;
        var settings = spec.Settings;
        var pages = SettingsPages.Build(settings, hasDocs: true);

        // Every formatter says where its options are documented, on the Advanced tab if it has one
        Assert.True(Uri.TryCreate(spec.DocsUrl, UriKind.Absolute, out var docs) && docs.Scheme == "https",
            $"{language} has no documentation link");
        Assert.Single(pages, p => p.ShowsDocs);
        Assert.Equal(
            settings.Any(s => s.Group == SettingsPages.AdvancedTitle) ? SettingsPages.AdvancedTitle : SettingsPages.AboutTitle,
            pages.Single(p => p.ShowsDocs).Title);
        Assert.True((spec.Note?.Length ?? 0) <= 100, "The note is one line");

        Assert.All(pages, p => Assert.True(p.Rows <= SettingsPages.MaxRows, $"Tab '{p.Title}' has {p.Rows} rows"));

        // Groups are sized by hand so that each is one tab; "Spacing 2" means a group outgrew its page
        Assert.All(pages, p => Assert.False(char.IsDigit(p.Title[^1]),
            $"Group '{p.Title[..^2]}' has more than {SettingsPages.MaxRows} rows: split it into named groups"));

        // The tab strip wraps; more than two rows of it pushes the page out of the window.
        // A tab is about 36 px of padding plus 8 px per character, in a 640 px wide dialog.
        var stripWidth = pages.Sum(p => 36 + 8 * p.Title.Length);
        Assert.True(stripWidth <= 2 * 600, $"Tabs need about {stripWidth} px, more than two rows");

        // A description that wraps onto a second line makes its row taller than the page allows for.
        // Text settings show theirs over the full width, on two lines at most.
        Assert.All(settings, s => Assert.True(
            (s.Description?.Length ?? 0) <= (s.Type == SettingType.Text ? 170 : 95),
            $"Description of '{s.DisplayName}' is {s.Description?.Length} characters long"));
    }
}
