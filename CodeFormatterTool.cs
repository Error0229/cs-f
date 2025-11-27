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
    private Language _selectedLanguage;

    [Import]
    private IFileStorage _fileStorage = null!;

    [Import]
    private IClipboard _clipboard = null!;

    public CodeFormatterTool()
    {
        _formatterService = new FormatterService(_configManager, new ProcessRunner());
        _selectedLanguage = _configManager.GetLastLanguage();
    }

    public UIToolView View
        => new UIToolView(
            Stack()
                .Vertical()
                .WithChildren(
                    // Toolbar
                    Stack()
                        .Horizontal()
                        .SmallSpacing()
                        .WithChildren(
                            SelectDropDownList("language-selector")
                                .Title("Language")
                                .WithItems(GetLanguageItems())
                                .Select((int)_selectedLanguage)
                                .OnItemSelected(OnLanguageSelected),
                            Button("format-btn")
                                .Text(CodeFormatterStrings.FormatButton)
                                .AccentAppearance()
                                .OnClick(OnFormatClickAsync),
                            Button("swap-btn")
                                .Text(CodeFormatterStrings.SwapButton)
                                .OnClick(OnSwapClick),
                            Button("clear-btn")
                                .Text(CodeFormatterStrings.ClearButton)
                                .OnClick(OnClearClick),
                            Button("load-btn")
                                .Text(CodeFormatterStrings.LoadButton)
                                .OnClick(OnLoadClickAsync),
                            Button("save-btn")
                                .Text(CodeFormatterStrings.SaveButton)
                                .OnClick(OnSaveClickAsync)),

                    // Side-by-side editors
                    SplitGrid()
                        .Horizontal()
                        .WithLeftPaneChild(
                            _inputEditor
                                .Title(CodeFormatterStrings.InputTitle)
                                .Language(GetMonacoLanguage(_selectedLanguage))
                                .AlwaysWrap())
                        .WithRightPaneChild(
                            _outputEditor
                                .Title(CodeFormatterStrings.OutputTitle)
                                .Language(GetMonacoLanguage(_selectedLanguage))
                                .ReadOnly()
                                .AlwaysWrap()
                                .CommandBarExtraContent(
                                    Button("copy-btn")
                                        .Text(CodeFormatterStrings.CopyButton)
                                        .OnClick(OnCopyClickAsync)))));

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        // Handle Smart Detection if needed
    }

    private static IUIDropDownListItem[] GetLanguageItems() =>
    [
        Item(Language.Python.ToDisplayName(), Language.Python),
        Item(Language.JavaScript.ToDisplayName(), Language.JavaScript),
        Item(Language.TypeScript.ToDisplayName(), Language.TypeScript),
        Item(Language.Java.ToDisplayName(), Language.Java),
        Item(Language.Sql.ToDisplayName(), Language.Sql)
    ];

    private static string GetMonacoLanguage(Language language) => language switch
    {
        Language.Python => "python",
        Language.JavaScript => "javascript",
        Language.TypeScript => "typescript",
        Language.Java => "java",
        Language.Sql => "sql",
        _ => "plaintext"
    };

    private void OnLanguageSelected(IUIDropDownListItem? item)
    {
        if (item?.Value is not Language language)
            return;

        _selectedLanguage = language;
        _configManager.SaveLastLanguage(language);

        var monacoLang = GetMonacoLanguage(language);
        _inputEditor.Language(monacoLang);
        _outputEditor.Language(monacoLang);
    }

    private async ValueTask OnFormatClickAsync()
    {
        var input = _inputEditor.Text;
        var result = await _formatterService.FormatAsync(input, _selectedLanguage);

        _outputEditor.Text(result.Output);
    }

    private void OnSwapClick()
    {
        var output = _outputEditor.Text;
        _inputEditor.Text(output);
        _outputEditor.Text(string.Empty);
    }

    private void OnClearClick()
    {
        _inputEditor.Text(string.Empty);
        _outputEditor.Text(string.Empty);
    }

    private async ValueTask OnLoadClickAsync()
    {
        using var file = await _fileStorage.PickOpenFileAsync("*");
        if (file is null)
            return;

        using var stream = await file.GetNewAccessToFileContentAsync(CancellationToken.None);
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        _inputEditor.Text(content);
    }

    private async ValueTask OnSaveClickAsync()
    {
        var output = _outputEditor.Text;
        if (string.IsNullOrWhiteSpace(output))
            return;

        using var stream = await _fileStorage.PickSaveFileAsync("*");
        if (stream is null)
            return;

        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(output);
    }

    private async ValueTask OnCopyClickAsync()
    {
        var output = _outputEditor.Text;
        if (!string.IsNullOrWhiteSpace(output))
            await _clipboard.SetClipboardTextAsync(output);
    }
}
