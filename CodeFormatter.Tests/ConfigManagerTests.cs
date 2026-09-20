using CodeFormatter.Formatters;
using CodeFormatter.Models;
using CodeFormatter.Services;

namespace CodeFormatter.Tests;

/// <summary>
/// config.toml files written by versions up to 1.2.0 must keep working.
/// </summary>
public class ConfigManagerTests
{
    private static ConfigManager FromToml(string toml, out string path)
    {
        path = TestFormatter.NewConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, toml);
        return new ConfigManager(path);
    }

    [Fact]
    public void ShippedDefaults_AreNotTreatedAsUserCommands()
    {
        // What 1.2.0 wrote on first run
        var config = FromToml("""
            [defaults]
            lastLanguage = "kotlin"

            [formatters.kotlin]
            command = "ktlint"
            args = ["--stdin", "--format"]

            [formatters.typescript]
            command = "dprint"
            args = ["fmt", "--stdin", "file.ts", "--plugins", "https://plugins.dprint.dev/typescript-0.93.0.wasm"]

            [formatters.php]
            command = "php-cs-fixer"
            args = ["fix", "{file}", "--rules=@PSR12", "--using-cache=no", "--quiet"]
            usesTempFile = true
            tempFileExtension = "php"

            [formatters.java]
            command = "powershell"
            args = ["-NoProfile", "-NonInteractive", "-Command", "& prettier --plugin=prettier-plugin-java --parser java"]
            requiresNode = true
            """, out _);

        Assert.Equal(Language.Kotlin, config.GetLastLanguage());
        Assert.Null(config.GetUserEntry(Language.Kotlin));
        Assert.Null(config.GetUserEntry(Language.TypeScript));
        Assert.Null(config.GetUserEntry(Language.Php));
        Assert.Null(config.GetUserEntry(Language.Java));
    }

    [Fact]
    public async Task UserCommand_IsRunAsWritten()
    {
        var config = FromToml("""
            [formatters.python]
            command = "ruff"
            args = ["format", "--line-length", "20", "-"]
            """, out _);

        var entry = config.GetUserEntry(Language.Python);
        Assert.NotNull(entry);
        Assert.Equal(["format", "--line-length", "20", "-"], entry.Args);

        var result = await TestFormatter.Create(config).FormatAsync("x = [1111111, 2222222, 3333333]", Language.Python);
        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("[\n", result.Output);
    }

    [Fact]
    public async Task UserCommand_WithTempFile_IsRunAsWritten()
    {
        var config = FromToml("""
            [formatters.r]
            command = "air"
            args = ["format", "{file}"]
            usesTempFile = true
            tempFileExtension = "R"

            [formatters.go]
            command = "gofumpt"
            args = ["-extra"]
            """, out _);

        // The first is a shipped default, the second is not
        Assert.Null(config.GetUserEntry(Language.R));
        Assert.NotNull(config.GetUserEntry(Language.Go));

        var result = await TestFormatter.Create(config).FormatAsync("package main\nfunc f(a int, b int) {}", Language.Go);
        Assert.True(result.Success, $"Format failed: {result.Output}");
        Assert.Contains("a, b int", result.Output);
    }

    [Fact]
    public void OldSettingKeys_AreMigrated()
    {
        var config = FromToml("""
            [formatters.python]
            command = "ruff"
            args = ["format", "-"]

            [formatters.python.settings]
            line-length = 100
            quote-style = "single"

            [formatters.shell.settings]
            indent = 4
            caseIndent = true

            [formatters.c.settings]
            style = "Google"

            [formatters.go.settings]
            extra = true
            """, out _);

        var python = config.GetSettingsWithDefaults(Language.Python);
        Assert.Equal(100, python["line-length"]);
        Assert.Equal("single", python["format.quote-style"]);

        var shell = config.GetSettingsWithDefaults(Language.Shell);
        Assert.Equal(4, shell["-i"]);
        Assert.Equal(true, shell["-ci"]);

        Assert.Equal("Google", config.GetSettingsWithDefaults(Language.C)["BasedOnStyle"]);
        Assert.Equal(true, config.GetSettingsWithDefaults(Language.Go)["-extra"]);
    }

    [Fact]
    public void InvalidSavedValues_FallBackToTheDefault()
    {
        // One bad option makes most formatters refuse to run at all
        var config = FromToml("""
            [formatters.python.settings]
            "format.quote-style" = "backtick"
            line-length = 100000
            nonsense = true
            """, out _);

        var settings = config.GetSettingsWithDefaults(Language.Python);
        Assert.Equal("double", settings["format.quote-style"]);
        Assert.Equal(88, settings["line-length"]);
        Assert.False(settings.ContainsKey("nonsense"));
        Assert.Empty(config.GetChangedSettings(Language.Python, FormatterSpecs.For(Language.Python)!));
    }

    [Fact]
    public void Save_WritesOnlyWhatDiffersFromTheDefault_AndRoundTrips()
    {
        var path = TestFormatter.NewConfigPath();
        var config = new ConfigManager(path);

        var settings = config.GetSettingsWithDefaults(Language.Python);
        settings["format.quote-style"] = "single";
        config.SaveAllSettings(Language.Python, settings);
        config.SaveAllSettings(Language.Shell, config.GetSettingsWithDefaults(Language.Shell));

        var toml = File.ReadAllText(path);
        Assert.Contains("\"format.quote-style\" = \"single\"", toml);
        Assert.DoesNotContain("line-length", toml);
        Assert.DoesNotContain("formatters.shell", toml);
        Assert.DoesNotContain(toml.Split('\n'), line => line.StartsWith("command"));

        var reloaded = new ConfigManager(path);
        Assert.Equal("single", reloaded.GetSettingsWithDefaults(Language.Python)["format.quote-style"]);
        Assert.Single(reloaded.GetChangedSettings(Language.Python, FormatterSpecs.For(Language.Python)!));
    }

    [Fact]
    public void EveryLanguage_HasAFormatter_AndConsistentSettings()
    {
        foreach (var info in LanguageRegistry.All)
        {
            var spec = FormatterSpecs.For(info.Language);
            Assert.True(spec is not null, $"{info.DisplayName} has no formatter");

            Assert.Equal(spec.Settings.Length, spec.Settings.Select(s => s.Key).Distinct().Count());
            foreach (var def in spec.Settings)
            {
                Assert.True(def.Accepts(def.DefaultValue),
                    $"{info.DisplayName}: default of '{def.Key}' is not a valid value for it");
            }
        }
    }
}
