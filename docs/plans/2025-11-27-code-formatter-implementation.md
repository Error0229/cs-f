# Code Formatter DevToys Plugin — Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build a DevToys plugin that formats code in Python, JavaScript, TypeScript, Java, and SQL using configurable external formatters.

**Architecture:** MEF-based DevToys extension with side-by-side UI (input/output panels), a FormatterService that shells out to CLI formatters (ruff, dprint, npx prettier/sql-formatter), and a ConfigManager that reads/writes TOML configuration.

**Tech Stack:** .NET 8, DevToys.Api 2.0.8, Tomlyn (TOML parser), System.Diagnostics.Process for CLI execution.

---

## Task 1: Project Setup & Dependencies

**Files:**
- Modify: `cs-f.csproj`
- Delete: `Class1.cs`

**Step 1: Add Tomlyn NuGet package**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet add package Tomlyn
```

Expected: Package added successfully.

**Step 2: Update csproj for DevToys extension**

Edit `cs-f.csproj` to:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>CodeFormatter</RootNamespace>
    <AssemblyName>CodeFormatter</AssemblyName>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="DevToys.Api" Version="2.0.8-preview" />
    <PackageReference Include="Tomlyn" Version="0.19.0" />
  </ItemGroup>

</Project>
```

**Step 3: Delete Class1.cs**

Run:
```bash
del C:\Users\cato\cs-f\Class1.cs
```

**Step 4: Verify build**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet build
```

Expected: Build succeeded.

**Step 5: Commit**

```bash
git add -A && git commit -m "chore: setup project for DevToys extension with Tomlyn"
```

---

## Task 2: Create Language Enum

**Files:**
- Create: `Models/Language.cs`

**Step 1: Create Models directory**

Run:
```bash
mkdir C:\Users\cato\cs-f\Models
```

**Step 2: Create Language.cs**

Create `Models/Language.cs`:
```csharp
namespace CodeFormatter.Models;

public enum Language
{
    Python,
    JavaScript,
    TypeScript,
    Java,
    Sql
}

public static class LanguageExtensions
{
    public static string ToDisplayName(this Language language) => language switch
    {
        Language.Python => "Python",
        Language.JavaScript => "JavaScript",
        Language.TypeScript => "TypeScript",
        Language.Java => "Java",
        Language.Sql => "SQL",
        _ => language.ToString()
    };

    public static string ToConfigKey(this Language language) => language switch
    {
        Language.Python => "python",
        Language.JavaScript => "javascript",
        Language.TypeScript => "typescript",
        Language.Java => "java",
        Language.Sql => "sql",
        _ => language.ToString().ToLowerInvariant()
    };
}
```

**Step 3: Verify build**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet build
```

Expected: Build succeeded.

**Step 4: Commit**

```bash
git add -A && git commit -m "feat: add Language enum with display names"
```

---

## Task 3: Create Configuration Models

**Files:**
- Create: `Models/FormatterConfig.cs`

**Step 1: Create FormatterConfig.cs**

Create `Models/FormatterConfig.cs`:
```csharp
namespace CodeFormatter.Models;

public class CodeFormatterConfig
{
    public DefaultsConfig Defaults { get; set; } = new();
    public Dictionary<string, FormatterEntry> Formatters { get; set; } = new();
    public PathsConfig? Paths { get; set; }
}

public class DefaultsConfig
{
    public string LastLanguage { get; set; } = "python";
}

public class FormatterEntry
{
    public string Command { get; set; } = string.Empty;
    public string[] Args { get; set; } = [];
    public bool RequiresNode { get; set; } = false;
}

public class PathsConfig
{
    public string? Ruff { get; set; }
    public string? Dprint { get; set; }
}
```

**Step 2: Verify build**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet build
```

Expected: Build succeeded.

**Step 3: Commit**

```bash
git add -A && git commit -m "feat: add configuration models for TOML parsing"
```

---

## Task 4: Create ConfigManager Service

**Files:**
- Create: `Services/ConfigManager.cs`

**Step 1: Create Services directory**

Run:
```bash
mkdir C:\Users\cato\cs-f\Services
```

**Step 2: Create ConfigManager.cs**

Create `Services/ConfigManager.cs`:
```csharp
using CodeFormatter.Models;
using Tomlyn;
using Tomlyn.Model;

namespace CodeFormatter.Services;

public class ConfigManager
{
    private readonly string _configPath;
    private CodeFormatterConfig? _cachedConfig;

    public ConfigManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, "DevToys", "CodeFormatter");
        _configPath = Path.Combine(configDir, "config.toml");
    }

    public CodeFormatterConfig LoadConfig()
    {
        if (_cachedConfig is not null)
            return _cachedConfig;

        if (!File.Exists(_configPath))
        {
            _cachedConfig = CreateDefaultConfig();
            SaveConfig(_cachedConfig);
            return _cachedConfig;
        }

        var toml = File.ReadAllText(_configPath);
        _cachedConfig = ParseConfig(toml);
        return _cachedConfig;
    }

    public void SaveLastLanguage(Language language)
    {
        var config = LoadConfig();
        config.Defaults.LastLanguage = language.ToConfigKey();
        SaveConfig(config);
    }

    public Language GetLastLanguage()
    {
        var config = LoadConfig();
        return config.Defaults.LastLanguage.ToLowerInvariant() switch
        {
            "python" => Language.Python,
            "javascript" => Language.JavaScript,
            "typescript" => Language.TypeScript,
            "java" => Language.Java,
            "sql" => Language.Sql,
            _ => Language.Python
        };
    }

    public FormatterEntry? GetFormatterEntry(Language language)
    {
        var config = LoadConfig();
        var key = language.ToConfigKey();
        return config.Formatters.TryGetValue(key, out var entry) ? entry : null;
    }

    public string? GetCustomPath(string formatter)
    {
        var config = LoadConfig();
        return formatter.ToLowerInvariant() switch
        {
            "ruff" => config.Paths?.Ruff,
            "dprint" => config.Paths?.Dprint,
            _ => null
        };
    }

    private void SaveConfig(CodeFormatterConfig config)
    {
        var dir = Path.GetDirectoryName(_configPath)!;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var toml = GenerateToml(config);
        File.WriteAllText(_configPath, toml);
    }

    private static CodeFormatterConfig CreateDefaultConfig() => new()
    {
        Defaults = new DefaultsConfig { LastLanguage = "python" },
        Formatters = new Dictionary<string, FormatterEntry>
        {
            ["python"] = new() { Command = "ruff", Args = ["format", "-"] },
            ["javascript"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.js"] },
            ["typescript"] = new() { Command = "dprint", Args = ["fmt", "--stdin", "file.ts"] },
            ["java"] = new() { Command = "npx", Args = ["prettier", "--parser", "java"], RequiresNode = true },
            ["sql"] = new() { Command = "npx", Args = ["sql-formatter", "--language", "postgresql"], RequiresNode = true }
        }
    };

    private static CodeFormatterConfig ParseConfig(string toml)
    {
        var model = Toml.ToModel(toml);
        var config = new CodeFormatterConfig();

        if (model.TryGetValue("defaults", out var defaultsObj) && defaultsObj is TomlTable defaults)
        {
            if (defaults.TryGetValue("lastLanguage", out var lastLang))
                config.Defaults.LastLanguage = lastLang?.ToString() ?? "python";
        }

        if (model.TryGetValue("formatters", out var formattersObj) && formattersObj is TomlTable formatters)
        {
            foreach (var (key, value) in formatters)
            {
                if (value is not TomlTable formatterTable)
                    continue;

                var entry = new FormatterEntry();
                if (formatterTable.TryGetValue("command", out var cmd))
                    entry.Command = cmd?.ToString() ?? "";
                if (formatterTable.TryGetValue("args", out var argsObj) && argsObj is TomlArray args)
                    entry.Args = args.Select(a => a?.ToString() ?? "").ToArray();
                if (formatterTable.TryGetValue("requiresNode", out var reqNode))
                    entry.RequiresNode = reqNode is bool b && b;

                config.Formatters[key] = entry;
            }
        }

        if (model.TryGetValue("paths", out var pathsObj) && pathsObj is TomlTable paths)
        {
            config.Paths = new PathsConfig();
            if (paths.TryGetValue("ruff", out var ruff))
                config.Paths.Ruff = ruff?.ToString();
            if (paths.TryGetValue("dprint", out var dprint))
                config.Paths.Dprint = dprint?.ToString();
        }

        return config;
    }

    private static string GenerateToml(CodeFormatterConfig config)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Code Formatter Configuration");
        sb.AppendLine();
        sb.AppendLine("[defaults]");
        sb.AppendLine($"lastLanguage = \"{config.Defaults.LastLanguage}\"");
        sb.AppendLine();

        foreach (var (key, entry) in config.Formatters)
        {
            sb.AppendLine($"[formatters.{key}]");
            sb.AppendLine($"command = \"{entry.Command}\"");
            var argsStr = string.Join(", ", entry.Args.Select(a => $"\"{a}\""));
            sb.AppendLine($"args = [{argsStr}]");
            if (entry.RequiresNode)
                sb.AppendLine("requiresNode = true");
            sb.AppendLine();
        }

        if (config.Paths is not null)
        {
            sb.AppendLine("[paths]");
            if (config.Paths.Ruff is not null)
                sb.AppendLine($"ruff = \"{config.Paths.Ruff}\"");
            if (config.Paths.Dprint is not null)
                sb.AppendLine($"dprint = \"{config.Paths.Dprint}\"");
        }

        return sb.ToString();
    }
}
```

**Step 3: Verify build**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet build
```

Expected: Build succeeded.

**Step 4: Commit**

```bash
git add -A && git commit -m "feat: add ConfigManager for TOML config handling"
```

---

## Task 5: Create ProcessRunner Service

**Files:**
- Create: `Services/ProcessRunner.cs`

**Step 1: Create ProcessRunner.cs**

Create `Services/ProcessRunner.cs`:
```csharp
using System.Diagnostics;

namespace CodeFormatter.Services;

public record ProcessResult(bool Success, string Output, string Error);

public class ProcessRunner
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

    public async Task<ProcessResult> RunAsync(string command, string[] args, string input, CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        try
        {
            process.Start();

            await process.StandardInput.WriteAsync(input);
            process.StandardInput.Close();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);

            var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

            await process.WaitForExitAsync(cts.Token);

            var output = await outputTask;
            var error = await errorTask;

            return new ProcessResult(process.ExitCode == 0, output, error);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            return new ProcessResult(false, "", "Formatting timed out. Code may be too large.");
        }
        catch (Exception ex)
        {
            return new ProcessResult(false, "", $"Failed to run formatter: {ex.Message}");
        }
    }

    public bool IsNodeInstalled()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "node",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit(2000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
```

**Step 2: Verify build**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet build
```

Expected: Build succeeded.

**Step 3: Commit**

```bash
git add -A && git commit -m "feat: add ProcessRunner for CLI formatter execution"
```

---

## Task 6: Create FormatterService

**Files:**
- Create: `Services/FormatterService.cs`

**Step 1: Create FormatterService.cs**

Create `Services/FormatterService.cs`:
```csharp
using CodeFormatter.Models;

namespace CodeFormatter.Services;

public record FormatResult(bool Success, string Output);

public class FormatterService
{
    private readonly ConfigManager _configManager;
    private readonly ProcessRunner _processRunner;
    private readonly string _bundledBinariesPath;

    public FormatterService(ConfigManager configManager, ProcessRunner processRunner)
    {
        _configManager = configManager;
        _processRunner = processRunner;
        _bundledBinariesPath = Path.Combine(AppContext.BaseDirectory, "Binaries");
    }

    public async Task<FormatResult> FormatAsync(string code, Language language, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new FormatResult(false, "No code to format.");

        var entry = _configManager.GetFormatterEntry(language);
        if (entry is null)
            return new FormatResult(false, $"No formatter configured for {language.ToDisplayName()}.");

        if (entry.RequiresNode && !_processRunner.IsNodeInstalled())
        {
            return new FormatResult(false,
                $"Node.js Required\n\n" +
                $"{language.ToDisplayName()} formatting requires Node.js to be installed.\n\n" +
                $"Download: https://nodejs.org");
        }

        var command = ResolveCommand(entry.Command);
        var result = await _processRunner.RunAsync(command, entry.Args, code, cancellationToken);

        if (!result.Success)
        {
            var errorMessage = string.IsNullOrWhiteSpace(result.Error)
                ? "Unknown formatting error occurred."
                : result.Error;
            return new FormatResult(false, $"Formatting Error\n\n{errorMessage}");
        }

        return new FormatResult(true, result.Output);
    }

    private string ResolveCommand(string command)
    {
        // Check for custom path in config
        var customPath = _configManager.GetCustomPath(command);
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
            return customPath;

        // Check for bundled binary
        var bundledPath = Path.Combine(_bundledBinariesPath, $"{command}.exe");
        if (File.Exists(bundledPath))
            return bundledPath;

        // Fall back to PATH lookup
        return command;
    }
}
```

**Step 2: Verify build**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet build
```

Expected: Build succeeded.

**Step 3: Commit**

```bash
git add -A && git commit -m "feat: add FormatterService to orchestrate formatting"
```

---

## Task 7: Create Resource File

**Files:**
- Create: `Resources/CodeFormatterStrings.resx`

**Step 1: Create Resources directory**

Run:
```bash
mkdir C:\Users\cato\cs-f\Resources
```

**Step 2: Create CodeFormatterStrings.resx**

Create `Resources/CodeFormatterStrings.resx`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" msdata:Ordinal="1" />
              <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
              <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name="resmimetype">
    <value>text/microsoft-resx</value>
  </resheader>
  <resheader name="version">
    <value>1.3</value>
  </resheader>
  <resheader name="reader">
    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <resheader name="writer">
    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>
  </resheader>
  <data name="ShortDisplayTitle" xml:space="preserve">
    <value>Code Formatter</value>
  </data>
  <data name="LongDisplayTitle" xml:space="preserve">
    <value>Code Formatter</value>
  </data>
  <data name="Description" xml:space="preserve">
    <value>Format code in multiple languages using configurable formatters</value>
  </data>
  <data name="AccessibleName" xml:space="preserve">
    <value>Code Formatter Tool</value>
  </data>
  <data name="InputTitle" xml:space="preserve">
    <value>Input</value>
  </data>
  <data name="OutputTitle" xml:space="preserve">
    <value>Output</value>
  </data>
  <data name="FormatButton" xml:space="preserve">
    <value>Format</value>
  </data>
  <data name="SwapButton" xml:space="preserve">
    <value>Swap</value>
  </data>
  <data name="ClearButton" xml:space="preserve">
    <value>Clear</value>
  </data>
  <data name="LoadButton" xml:space="preserve">
    <value>Load</value>
  </data>
  <data name="SaveButton" xml:space="preserve">
    <value>Save</value>
  </data>
  <data name="CopyButton" xml:space="preserve">
    <value>Copy</value>
  </data>
</root>
```

**Step 3: Verify build**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet build
```

Expected: Build succeeded.

**Step 4: Commit**

```bash
git add -A && git commit -m "feat: add resource file for localized strings"
```

---

## Task 8: Create Resource Assembly Identifier

**Files:**
- Create: `CodeFormatterResourceIdentifier.cs`

**Step 1: Create CodeFormatterResourceIdentifier.cs**

Create `CodeFormatterResourceIdentifier.cs`:
```csharp
using DevToys.Api;
using System.ComponentModel.Composition;

namespace CodeFormatter;

[Export(typeof(IResourceAssemblyIdentifier))]
[Name(nameof(CodeFormatterResourceIdentifier))]
internal sealed class CodeFormatterResourceIdentifier : IResourceAssemblyIdentifier
{
    public ValueTask<FontDefinition[]> GetFontDefinitionsAsync()
    {
        return new ValueTask<FontDefinition[]>([]);
    }
}
```

**Step 2: Verify build**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet build
```

Expected: Build succeeded.

**Step 3: Commit**

```bash
git add -A && git commit -m "feat: add MEF resource assembly identifier"
```

---

## Task 9: Create Main GUI Tool

**Files:**
- Create: `CodeFormatterTool.cs`

**Step 1: Create CodeFormatterTool.cs**

Create `CodeFormatterTool.cs`:
```csharp
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
```

**Step 2: Verify build**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet build
```

Expected: Build succeeded. (May have warnings about nullable - acceptable)

**Step 3: Commit**

```bash
git add -A && git commit -m "feat: add main CodeFormatterTool GUI implementation"
```

---

## Task 10: Create Binaries Directory Placeholder

**Files:**
- Create: `Binaries/.gitkeep`
- Modify: `cs-f.csproj` (add copy to output)

**Step 1: Create Binaries directory**

Run:
```bash
mkdir C:\Users\cato\cs-f\Binaries
```

**Step 2: Create placeholder file**

Create `Binaries/.gitkeep`:
```
# Place ruff.exe and dprint.exe here
# Download from:
# - Ruff: https://github.com/astral-sh/ruff/releases
# - dprint: https://github.com/dprint/dprint/releases
```

**Step 3: Update csproj to copy binaries**

Edit `cs-f.csproj` to add:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>CodeFormatter</RootNamespace>
    <AssemblyName>CodeFormatter</AssemblyName>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="DevToys.Api" Version="2.0.8-preview" />
    <PackageReference Include="Tomlyn" Version="0.19.0" />
  </ItemGroup>

  <ItemGroup>
    <None Include="Binaries\**\*" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

**Step 4: Verify build**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet build
```

Expected: Build succeeded.

**Step 5: Commit**

```bash
git add -A && git commit -m "chore: add Binaries directory for bundled formatters"
```

---

## Task 11: Download and Bundle Formatters

**Files:**
- Download: `Binaries/ruff.exe`
- Download: `Binaries/dprint.exe`

**Step 1: Download Ruff**

Run (PowerShell):
```powershell
$ruffUrl = "https://github.com/astral-sh/ruff/releases/latest/download/ruff-x86_64-pc-windows-msvc.zip"
$ruffZip = "$env:TEMP\ruff.zip"
Invoke-WebRequest -Uri $ruffUrl -OutFile $ruffZip
Expand-Archive -Path $ruffZip -DestinationPath "$env:TEMP\ruff" -Force
Copy-Item "$env:TEMP\ruff\ruff.exe" "C:\Users\cato\cs-f\Binaries\ruff.exe"
```

**Step 2: Download dprint**

Run (PowerShell):
```powershell
$dprintUrl = "https://github.com/dprint/dprint/releases/latest/download/dprint-x86_64-pc-windows-msvc.zip"
$dprintZip = "$env:TEMP\dprint.zip"
Invoke-WebRequest -Uri $dprintUrl -OutFile $dprintZip
Expand-Archive -Path $dprintZip -DestinationPath "$env:TEMP\dprint" -Force
Copy-Item "$env:TEMP\dprint\dprint.exe" "C:\Users\cato\cs-f\Binaries\dprint.exe"
```

**Step 3: Verify binaries exist**

Run:
```bash
dir C:\Users\cato\cs-f\Binaries
```

Expected: `ruff.exe` and `dprint.exe` listed.

**Step 4: Add to gitignore (binaries are large)**

Create `.gitignore`:
```
Binaries/*.exe
bin/
obj/
.vs/
```

**Step 5: Commit**

```bash
git add -A && git commit -m "chore: add gitignore, document binary downloads"
```

---

## Task 12: Test the Extension

**Step 1: Build in Release mode**

Run:
```bash
cd C:\Users\cato\cs-f && dotnet build -c Release
```

Expected: Build succeeded.

**Step 2: Locate output**

Run:
```bash
dir C:\Users\cato\cs-f\bin\Release\net8.0
```

Expected: `CodeFormatter.dll` and bundled binaries present.

**Step 3: Install to DevToys**

Copy the output folder to DevToys extensions directory:
```powershell
$devtoysExtensions = "$env:LOCALAPPDATA\DevToys\Extensions"
New-Item -ItemType Directory -Path $devtoysExtensions -Force
Copy-Item -Path "C:\Users\cato\cs-f\bin\Release\net8.0\*" -Destination "$devtoysExtensions\CodeFormatter" -Recurse -Force
```

**Step 4: Launch DevToys and test**

1. Open DevToys
2. Find "Code Formatter" in Formatters group
3. Paste Python code, select Python, click Format
4. Verify formatting works

**Step 5: Commit final state**

```bash
git add -A && git commit -m "chore: complete initial implementation"
```

---

## Summary

| Task | Description | Files |
|------|-------------|-------|
| 1 | Project setup | csproj |
| 2 | Language enum | Models/Language.cs |
| 3 | Config models | Models/FormatterConfig.cs |
| 4 | ConfigManager | Services/ConfigManager.cs |
| 5 | ProcessRunner | Services/ProcessRunner.cs |
| 6 | FormatterService | Services/FormatterService.cs |
| 7 | Resource file | Resources/CodeFormatterStrings.resx |
| 8 | Resource identifier | CodeFormatterResourceIdentifier.cs |
| 9 | Main GUI tool | CodeFormatterTool.cs |
| 10 | Binaries setup | Binaries/, csproj |
| 11 | Download formatters | Binaries/*.exe |
| 12 | Test & deploy | - |

Total: ~12 tasks, each 2-10 minutes.
