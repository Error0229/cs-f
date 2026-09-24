using CodeFormatter.Formatters;
using CodeFormatter.Models;
using CodeFormatter.Resources;
using CodeFormatter.Services;
using DevToys.Api;
using System.ComponentModel.Composition;
using static DevToys.Api.GUI;

namespace CodeFormatter;

[Export(typeof(IGuiTool))]
[Name("CodeFormatter")]
[ToolDisplayInformation(
    IconFontName = "CodeFormatterIcons",
    IconGlyph = '\uE000',
    GroupName = PredefinedCommonToolGroupNames.Formatters,
    ResourceManagerAssemblyIdentifier = nameof(CodeFormatterResourceIdentifier),
    ResourceManagerBaseName = "CodeFormatter.Resources.CodeFormatterStrings",
    ShortDisplayTitleResourceName = nameof(CodeFormatterStrings.ShortDisplayTitle),
    LongDisplayTitleResourceName = nameof(CodeFormatterStrings.LongDisplayTitle),
    DescriptionResourceName = nameof(CodeFormatterStrings.Description),
    AccessibleNameResourceName = nameof(CodeFormatterStrings.AccessibleName))]
internal sealed class CodeFormatterTool : IGuiTool
{
    private readonly ConfigManager _configManager = new();
    private readonly FormatterService _formatterService;
    private readonly IUIMultiLineTextInput _inputEditor = MultiLineTextInput("input-editor");
    private readonly IUIMultiLineTextInput _outputEditor = MultiLineTextInput("output-editor");
    private readonly UIToolView _view = new();
    private Language _selectedLanguage;

    // Config dialog state
    private Dictionary<string, object> _pendingSettings = new();

    // Auto-format debounce
    private CancellationTokenSource? _formatCts;
    private const int DebounceDelayMs = 500;
    // For formatters that need seconds to start: do not launch one at every pause in typing
    private const int SlowFormatterDebounceDelayMs = 1200;

    [Import]
    private IFileStorage _fileStorage = null!;

    public CodeFormatterTool()
    {
        _formatterService = new FormatterService(_configManager, new ProcessRunner());
        _selectedLanguage = _configManager.GetLastLanguage();
    }

    public UIToolView View
    {
        get
        {
            if (_view.RootElement is null)
            {
                _view.WithRootElement(
                    Grid()
                        .Rows(
                            (GridRow.Settings, UIGridLength.Auto),
                            (GridRow.Content, new UIGridLength(1, UIGridUnitType.Fraction)))
                        .Columns(
                            (GridColumn.Stretch, new UIGridLength(1, UIGridUnitType.Fraction)))
                        .Cells(
                            // Toolbar row
                            Cell(
                                GridRow.Settings,
                                GridColumn.Stretch,
                                Stack()
                                    .Horizontal()
                                    .MediumSpacing()
                                    .AlignVertically(UIVerticalAlignment.Center)
                                    .WithChildren(
                                        Label().Text("Language"),
                                        SelectDropDownList("language-selector")
                                            .WithItems(GetLanguageItems())
                                            // The list follows the registry's order, not the enum's
                                            .Select(Math.Max(0, LanguageRegistry.All.ToList().FindIndex(info => info.Language == _selectedLanguage)))
                                            .OnItemSelected(OnLanguageSelectedAsync),
                                        Button("swap-btn")
                                            .Text(CodeFormatterStrings.SwapButton)
                                            .OnClick(OnSwapClick),
                                        Button("clear-btn")
                                            .Text(CodeFormatterStrings.ClearButton)
                                            .OnClick(OnClearClick),
                                        Button("config-btn")
                                            .Icon("FluentSystemIcons", '\uF6A9')
                                            .OnClick(OnConfigClickAsync))),
                            // Editors row
                            Cell(
                                GridRow.Content,
                                GridColumn.Stretch,
                                SplitGrid()
                                    .Horizontal()
                                    .WithLeftPaneChild(
                                        _inputEditor
                                            .Title(CodeFormatterStrings.InputTitle)
                                            .Language(GetMonacoLanguage(_selectedLanguage))
                                            .AlwaysWrap()
                                            .Extendable()
                                            .OnTextChanged(OnInputTextChangedAsync)
                                            .CommandBarExtraContent(
                                                Button("load-btn")
                                                    .Text(CodeFormatterStrings.LoadButton)
                                                    .OnClick(OnLoadClickAsync)))
                                    .WithRightPaneChild(
                                        _outputEditor
                                            .Title(CodeFormatterStrings.OutputTitle)
                                            .Language(GetMonacoLanguage(_selectedLanguage))
                                            .ReadOnly()
                                            .AlwaysWrap()
                                            .Extendable()))));
            }

            return _view;
        }
    }

    private enum GridRow { Settings, Content }
    private enum GridColumn { Stretch }

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        // Handle Smart Detection if needed
    }

    private static IUIDropDownListItem[] GetLanguageItems() =>
        LanguageRegistry.All
            .Select(info => Item(info.DisplayName, info.Language))
            .ToArray();

    private static string GetMonacoLanguage(Language language) =>
        language.ToMonacoLanguage();

    private async void OnLanguageSelectedAsync(IUIDropDownListItem? item)
    {
        if (item?.Value is not Language language)
            return;

        _selectedLanguage = language;
        _configManager.SaveLastLanguage(language);

        var monacoLang = GetMonacoLanguage(language);
        _inputEditor.Language(monacoLang);
        _outputEditor.Language(monacoLang);

        // Re-format with new language if there's input
        await FormatWithDebounceAsync();
    }

    private async void OnInputTextChangedAsync(string text)
    {
        await FormatWithDebounceAsync();
    }

    private async Task FormatWithDebounceAsync()
    {
        // Cancel any pending format operation
        _formatCts?.Cancel();
        _formatCts = new CancellationTokenSource();
        var token = _formatCts.Token;

        try
        {
            // Wait for debounce delay
            var slow = FormatterSpecs.For(_selectedLanguage)?.SlowToStart == true;
            await Task.Delay(slow ? SlowFormatterDebounceDelayMs : DebounceDelayMs, token);

            var input = _inputEditor.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                _outputEditor.Text(string.Empty);
                return;
            }

            // The token also stops the formatter process: the slow ones take seconds to start,
            // and would otherwise pile up behind every pause in typing
            var result = await _formatterService.FormatAsync(input, _selectedLanguage, token);

            // Check if cancelled before updating UI
            if (!token.IsCancellationRequested)
            {
                _outputEditor.Text(result.Output);
            }
        }
        catch (OperationCanceledException)
        {
            // Debounce cancelled, ignore
        }
    }

    private async void OnSwapClick()
    {
        var output = _outputEditor.Text;
        _inputEditor.Text(output);
        _outputEditor.Text(string.Empty);
        // Auto-format will trigger from OnTextChanged
    }

    private void OnClearClick()
    {
        _inputEditor.Text(string.Empty);
        _outputEditor.Text(string.Empty);
    }

    private async ValueTask OnLoadClickAsync()
    {
        var extensions = GetFileExtensions();
        using var file = await _fileStorage.PickOpenFileAsync(extensions);
        if (file is null)
            return;

        using var stream = await file.GetNewAccessToFileContentAsync(CancellationToken.None);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        _inputEditor.Text(content);
    }

    private static string[] GetFileExtensions() =>
    [
        .. LanguageRegistry.All
            .Select(info => info.FileExtension.Split('.')[^1].ToLowerInvariant())
            .Where(extension => extension != "dockerfile"),
        "tsx", "jsx", "yml", "gql", "h", "hpp", "cc", "kts", "sty", "cls", "txt"
    ];

    #region Config Dialog

    private async ValueTask OnConfigClickAsync()
    {
        _pendingSettings = _configManager.GetSettingsWithDefaults(_selectedLanguage);
        await OpenConfigDialogAsync();
    }

    // A DevToys dialog sizes itself to its content and does not scroll, so its content is given
    // a fixed size: it must not change shape while open, and must fit the window.
    private const int DialogWidth = 640;
    // A row is a card: its content, the card's own padding, and the gap to the next row
    private const int SettingContentHeight = 44;
    private const int SettingRowHeight = 76;

    private enum DialogRow { Title, Tabs, Page }
    private enum DialogColumn { Main }

    private enum SettingRow { Only }
    private enum SettingColumn { Text, Control }

    private async Task OpenConfigDialogAsync()
    {
        var spec = FormatterSpecs.For(_selectedLanguage);
        var pages = SettingsPages.Build(spec?.Settings ?? [], hasDocs: spec?.DocsUrl is not null || spec?.Note is not null);

        // Every page is built once; choosing a tab shows one of them and hides the rest
        var pageViews = pages
            .Select(page => Stack().Vertical().SmallSpacing().WithChildren(
            [
                .. page.Settings.Select(BuildSetting),
                .. page.ShowsDocs ? new[] { BuildDocs(spec!) } : []
            ]))
            .ToArray();
        var tabs = new IUIButton[pages.Count];

        void SelectPage(int selected)
        {
            for (var i = 0; i < pageViews.Length; i++)
            {
                if (i == selected)
                {
                    pageViews[i].Show();
                    tabs[i].AccentAppearance();
                }
                else
                {
                    pageViews[i].Hide();
                    tabs[i].NeutralAppearance();
                }
            }
        }

        for (var i = 0; i < tabs.Length; i++)
        {
            var index = i;
            tabs[i] = Button($"settings-tab-{i}").Text(pages[i].Title).OnClick(() => SelectPage(index));
        }
        SelectPage(0);

        // The tallest page decides the height, for all of them
        var pageHeight = SettingRowHeight * Math.Max(1, pages.Select(p => p.Rows).DefaultIfEmpty(0).Max());

        IUIElement body = pages.Count > 0
            ? Stack().Vertical().WithChildren(pageViews)
            : Label().Style(UILabelStyle.Body).Text("No configurable settings for this formatter.");

        await _view.OpenDialogAsync(
            dialogContent:
                Grid()
                    .RowSmallSpacing()
                    .Rows(
                        (DialogRow.Title, UIGridLength.Auto),
                        (DialogRow.Tabs, UIGridLength.Auto),
                        (DialogRow.Page, new UIGridLength(pageHeight, UIGridUnitType.Pixel)))
                    .Columns(
                        (DialogColumn.Main, new UIGridLength(DialogWidth, UIGridUnitType.Pixel)))
                    .Cells(
                        Cell(DialogRow.Title, DialogColumn.Main,
                            Label()
                                .Style(UILabelStyle.Subtitle)
                                .Text($"{_selectedLanguage.ToDisplayName()} Settings")),
                        // A single page needs no tabs
                        Cell(DialogRow.Tabs, DialogColumn.Main,
                            pages.Count > 1
                                ? Wrap().SmallSpacing().WithChildren(tabs)
                                : Stack()),
                        Cell(DialogRow.Page, DialogColumn.Main, body)),
            footerContent:
                Stack()
                    .Horizontal()
                    .MediumSpacing()
                    .AlignHorizontally(UIHorizontalAlignment.Right)
                    .WithChildren(
                        Button("config-reset-btn")
                            .Text(CodeFormatterStrings.ConfigResetButton)
                            .OnClick(OnConfigResetClickAsync),
                        Button("config-save-btn")
                            .Text(CodeFormatterStrings.ConfigSaveButton)
                            .AccentAppearance()
                            .OnClick(OnConfigSaveClick)),
            isDismissible: true);
    }

    private IUIElement BuildSetting(SettingDefinition def)
    {
        var currentValue = _pendingSettings.TryGetValue(def.Key, out var val)
            ? val
            : def.DefaultValue;

        var text = Stack()
            .Vertical()
            .NoSpacing()
            .AlignVertically(UIVerticalAlignment.Center)
            .WithChildren(
                Label().Text(def.DisplayName),
                Label().Style(UILabelStyle.Caption).Text(def.Description ?? ""));

        // Free text needs the full width: title and description, then the input on its own line
        if (def.Type == SettingType.Text)
            return Card(Stack().Vertical().SmallSpacing().WithChildren(text, BuildTextSetting(def, currentValue)));

        var control = def.Type switch
        {
            SettingType.Boolean => BuildBooleanSetting(def, currentValue),
            SettingType.Integer => BuildIntegerSetting(def, currentValue),
            SettingType.Choice => BuildChoiceSetting(def, currentValue),
            _ => Label().Text($"Unknown setting type: {def.Key}")
        };

        // Not DevToys' own Setting element: that keeps a column free for an icon we do not have.
        // The row height is fixed so that a page is as tall as was planned for it.
        return Card(
            Grid()
                .Rows((SettingRow.Only, new UIGridLength(SettingContentHeight, UIGridUnitType.Pixel)))
                .Columns(
                    (SettingColumn.Text, new UIGridLength(1, UIGridUnitType.Fraction)),
                    (SettingColumn.Control, UIGridLength.Auto))
                .Cells(
                    Cell(SettingRow.Only, SettingColumn.Text, text),
                    Cell(SettingRow.Only, SettingColumn.Control,
                        control.AlignVertically(UIVerticalAlignment.Center).AlignHorizontally(UIHorizontalAlignment.Right))));
    }

    /// <summary>
    /// What there is to say about the formatter's options as a whole, and where they are documented.
    /// </summary>
    private static IUIElement BuildDocs(FormatterSpec spec)
    {
        var children = new List<IUIElement>();

        if (spec.Note is not null)
            children.Add(Label().Style(UILabelStyle.Caption).Text(spec.Note));

        if (spec.DocsUrl is { } url)
        {
            children.Add(
                Button("settings-docs-link")
                    .HyperlinkAppearance()
                    .Text($"{spec.Command} options on {new Uri(url).Host}")
                    .OnClick(() => OpenInBrowser(url)));
        }

        return Stack().Vertical().SmallSpacing().WithChildren(children.ToArray());
    }

    private static void OpenInBrowser(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // No browser to hand it to; the address is on the button
        }
    }

    // Setting keys are the formatters' own ("format.quote-style", "-i"); element ids are plainer
    private static string ToId(string key) =>
        new(key.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());

    private IUIElement BuildBooleanSetting(SettingDefinition def, object currentValue)
    {
        var sw = Switch($"setting-{ToId(def.Key)}")
            .OnText("Yes")
            .OffText("No")
            .OnToggle(value => _pendingSettings[def.Key] = value);

        return currentValue is true ? sw.On() : sw.Off();
    }

    private IUIElement BuildIntegerSetting(SettingDefinition def, object currentValue)
    {
        return NumberInput($"setting-{ToId(def.Key)}")
            .HideCommandBar()
            .Minimum(def.Min ?? 1)
            .Maximum(def.Max ?? 1000)
            .Value(currentValue as int? ?? (int)def.DefaultValue)
            .OnValueChanged(value => _pendingSettings[def.Key] = (int)value);
    }

    private IUIElement BuildChoiceSetting(SettingDefinition def, object currentValue)
    {
        var choices = def.Choices ?? [];
        var items = choices.Select(c => Item(c, c)).ToArray();
        var selectedIndex = Math.Max(0, Array.IndexOf(choices, currentValue as string));

        return SelectDropDownList($"setting-{ToId(def.Key)}")
            .WithItems(items)
            .Select(selectedIndex)
            .OnItemSelected(item =>
            {
                if (item?.Value is string s)
                    _pendingSettings[def.Key] = s;
            });
    }

    private IUIElement BuildTextSetting(SettingDefinition def, object currentValue)
    {
        return SingleLineTextInput($"setting-{ToId(def.Key)}")
            .HideCommandBar()
            .Text(currentValue as string ?? "")
            .OnTextChanged(value => _pendingSettings[def.Key] = value);
    }

    private void OnConfigSaveClick()
    {
        _configManager.SaveAllSettings(_selectedLanguage, _pendingSettings);
        _view.CurrentOpenedDialog?.Close();

        // Show what the new settings do
        _ = FormatWithDebounceAsync();
    }

    private async void OnConfigResetClickAsync()
    {
        _configManager.ResetSettings(_selectedLanguage);
        _pendingSettings = _configManager.GetSettingsWithDefaults(_selectedLanguage);

        // Reopen dialog to refresh UI
        _view.CurrentOpenedDialog?.Close();
        await OpenConfigDialogAsync();
    }

    #endregion
}
