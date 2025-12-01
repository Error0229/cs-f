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
    IconFontName = "FluentSystemIcons",
    IconGlyph = '\uE943',
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
    private Language _configSelectedLanguage;
    private Dictionary<string, object> _pendingSettings = new();

    // Auto-format debounce
    private CancellationTokenSource? _formatCts;
    private const int DebounceDelayMs = 500;

    [Import]
    private IFileStorage _fileStorage = null!;

    public CodeFormatterTool()
    {
        _formatterService = new FormatterService(_configManager, new ProcessRunner());
        _selectedLanguage = _configManager.GetLastLanguage();
        _configSelectedLanguage = _selectedLanguage;
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
                                            .Select((int)_selectedLanguage)
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
    [
        Item(Language.Python.ToDisplayName(), Language.Python),
        Item(Language.JavaScript.ToDisplayName(), Language.JavaScript),
        Item(Language.TypeScript.ToDisplayName(), Language.TypeScript),
        Item(Language.Json.ToDisplayName(), Language.Json),
        Item(Language.Markdown.ToDisplayName(), Language.Markdown),
        Item(Language.Toml.ToDisplayName(), Language.Toml),
        Item(Language.Css.ToDisplayName(), Language.Css),
        Item(Language.Scss.ToDisplayName(), Language.Scss),
        Item(Language.Less.ToDisplayName(), Language.Less),
        Item(Language.Html.ToDisplayName(), Language.Html),
        Item(Language.Vue.ToDisplayName(), Language.Vue),
        Item(Language.Svelte.ToDisplayName(), Language.Svelte),
        Item(Language.Astro.ToDisplayName(), Language.Astro),
        Item(Language.Yaml.ToDisplayName(), Language.Yaml),
        Item(Language.GraphQL.ToDisplayName(), Language.GraphQL),
        Item(Language.Dockerfile.ToDisplayName(), Language.Dockerfile),
        Item(Language.Java.ToDisplayName(), Language.Java),
        Item(Language.Sql.ToDisplayName(), Language.Sql)
    ];

    private static string GetMonacoLanguage(Language language) => language switch
    {
        Language.Python => "python",
        Language.JavaScript => "javascript",
        Language.TypeScript => "typescript",
        Language.Json => "json",
        Language.Markdown => "markdown",
        Language.Toml => "toml",
        Language.Css => "css",
        Language.Scss => "scss",
        Language.Less => "less",
        Language.Html => "html",
        Language.Vue => "html",
        Language.Svelte => "html",
        Language.Astro => "html",
        Language.Yaml => "yaml",
        Language.GraphQL => "graphql",
        Language.Dockerfile => "dockerfile",
        Language.Java => "java",
        Language.Sql => "sql",
        _ => "plaintext"
    };

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
            await Task.Delay(DebounceDelayMs, token);

            var input = _inputEditor.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                _outputEditor.Text(string.Empty);
                return;
            }

            var result = await _formatterService.FormatAsync(input, _selectedLanguage);

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

    private string[] GetFileExtensions() =>
    [
        "py", "js", "ts", "tsx", "jsx", "json", "md", "toml",
        "css", "scss", "less", "html", "vue", "svelte", "astro",
        "yaml", "yml", "graphql", "gql", "java", "sql",
        "txt", "xml", "config"
    ];

    #region Config Dialog

    private async ValueTask OnConfigClickAsync()
    {
        _configSelectedLanguage = _selectedLanguage;
        _pendingSettings = _configManager.GetSettingsWithDefaults(_configSelectedLanguage);

        await OpenConfigDialogAsync();
    }

    private async Task OpenConfigDialogAsync()
    {
        var definitions = FormatterSettingsDefinitions.GetSettings(_configSelectedLanguage);
        var settingsControls = BuildSettingsControls(definitions);

        await _view.OpenDialogAsync(
            dialogContent:
                Stack()
                    .Vertical()
                    .LargeSpacing()
                    .WithChildren(
                        Label()
                            .Style(UILabelStyle.Subtitle)
                            .Text($"{_configSelectedLanguage.ToDisplayName()} Settings"),

                        // Language selector
                        Stack()
                            .Horizontal()
                            .SmallSpacing()
                            .AlignVertically(UIVerticalAlignment.Center)
                            .WithChildren(
                                Label().Text("Language:"),
                                SelectDropDownList("config-language")
                                    .WithItems(GetLanguageItems())
                                    .Select((int)_configSelectedLanguage)
                                    .OnItemSelected(OnConfigLanguageSelectedAsync)),

                        // Settings section
                        settingsControls.Length > 0
                            ? Stack()
                                .Vertical()
                                .MediumSpacing()
                                .WithChildren(settingsControls)
                            : Label()
                                .Style(UILabelStyle.Body)
                                .Text("No configurable settings for this formatter.")),
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

    private IUIElement[] BuildSettingsControls(SettingDefinition[] definitions)
    {
        var controls = new List<IUIElement>();

        foreach (var def in definitions)
        {
            var currentValue = _pendingSettings.TryGetValue(def.Key, out var val)
                ? val
                : def.DefaultValue;

            IUIElement control = def.Type switch
            {
                SettingType.Boolean => BuildBooleanSetting(def, currentValue),
                SettingType.Integer => BuildIntegerSetting(def, currentValue),
                SettingType.Choice => BuildChoiceSetting(def, currentValue),
                _ => Label().Text($"Unknown setting type: {def.Key}")
            };

            controls.Add(control);
        }

        return controls.ToArray();
    }

    private IUIElement BuildBooleanSetting(SettingDefinition def, object currentValue)
    {
        var isOn = currentValue is bool b && b;
        var sw = Switch($"setting-{def.Key}")
            .OnText("Yes")
            .OffText("No")
            .OnToggle(value => _pendingSettings[def.Key] = value);

        if (isOn)
            sw.On();
        else
            sw.Off();

        return Stack()
            .Horizontal()
            .SmallSpacing()
            .AlignVertically(UIVerticalAlignment.Center)
            .WithChildren(
                Label().Text(def.DisplayName),
                sw,
                def.Description != null
                    ? Label().Style(UILabelStyle.Caption).Text($"({def.Description})")
                    : Label().Text(""));
    }

    private IUIElement BuildIntegerSetting(SettingDefinition def, object currentValue)
    {
        var intValue = currentValue switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            _ => (int)def.DefaultValue
        };

        return Stack()
            .Vertical()
            .SmallSpacing()
            .WithChildren(
                Label().Text(def.DisplayName + (def.Description != null ? $" ({def.Description})" : "")),
                NumberInput($"setting-{def.Key}")
                    .Minimum(def.Min ?? 1)
                    .Maximum(def.Max ?? 1000)
                    .Value(intValue)
                    .OnValueChanged(value => _pendingSettings[def.Key] = (int)value));
    }

    private IUIElement BuildChoiceSetting(SettingDefinition def, object currentValue)
    {
        var choices = def.Choices ?? [];
        var items = choices.Select(c => Item(c, c)).ToArray();
        var currentStr = currentValue?.ToString() ?? def.DefaultValue.ToString();
        var selectedIndex = Array.IndexOf(choices, currentStr);
        if (selectedIndex < 0) selectedIndex = 0;

        return Stack()
            .Vertical()
            .SmallSpacing()
            .WithChildren(
                Label().Text(def.DisplayName + (def.Description != null ? $" ({def.Description})" : "")),
                SelectDropDownList($"setting-{def.Key}")
                    .WithItems(items)
                    .Select(selectedIndex)
                    .OnItemSelected(item =>
                    {
                        if (item?.Value is string s)
                            _pendingSettings[def.Key] = s;
                    }));
    }

    private async void OnConfigLanguageSelectedAsync(IUIDropDownListItem? item)
    {
        if (item?.Value is not Language language || language == _configSelectedLanguage)
            return;

        _configSelectedLanguage = language;
        _pendingSettings = _configManager.GetSettingsWithDefaults(_configSelectedLanguage);

        // Close and reopen dialog with new settings
        _view.CurrentOpenedDialog?.Close();
        await OpenConfigDialogAsync();
    }

    private void OnConfigSaveClick()
    {
        _configManager.SaveAllSettings(_configSelectedLanguage, _pendingSettings);
        _view.CurrentOpenedDialog?.Close();
    }

    private async void OnConfigResetClickAsync()
    {
        _configManager.ResetSettings(_configSelectedLanguage);
        _pendingSettings = _configManager.GetSettingsWithDefaults(_configSelectedLanguage);

        // Reopen dialog to refresh UI
        _view.CurrentOpenedDialog?.Close();
        await OpenConfigDialogAsync();
    }

    #endregion
}
