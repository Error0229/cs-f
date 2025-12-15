using CodeFormatter.Models;

namespace CodeFormatter.Services;

public record FormatResult(bool Success, string Output);

public class FormatterService
{
    private readonly ConfigManager _configManager;
    private readonly ProcessRunner _processRunner;
    private readonly string[] _binarySearchPaths;

    public FormatterService(ConfigManager configManager, ProcessRunner processRunner)
    {
        _configManager = configManager;
        _processRunner = processRunner;

        // Get the directory where this assembly is located (the plugin's directory)
        // Assembly is at: CodeFormatter/lib/net8.0/CodeFormatter.dll
        // Plugin root is: CodeFormatter/
        var assemblyDir = Path.GetDirectoryName(typeof(FormatterService).Assembly.Location) ?? "";
        var libDir = Path.GetDirectoryName(assemblyDir) ?? assemblyDir;      // Go up from net8.0 to lib
        var pluginRoot = Path.GetDirectoryName(libDir) ?? libDir;            // Go up from lib to plugin root

        // Search paths for bundled binaries (in order of preference)
        _binarySearchPaths =
        [
            Path.Combine(assemblyDir, "Binaries"),                           // Local development (next to DLL)
            Path.Combine(pluginRoot, "runtimes", "win-x64", "native"),        // NuGet package (Windows x64)
            Path.Combine(pluginRoot, "runtimes", "win", "native"),            // NuGet package (Windows any)
            Path.Combine(AppContext.BaseDirectory, "Binaries"),              // Fallback: app base directory
            assemblyDir                                                      // Direct in assembly directory
        ];
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

        // Check for required npm packages before running
        if (entry.RequiresNode)
        {
            var packageCheck = CheckRequiredNpmPackages(language);
            if (!packageCheck.Success)
                return packageCheck;
        }

        var command = ResolveCommand(entry.Command);

        // For Node.js-based formatters, use the global npm directory as working directory
        // so that prettier can find plugins like prettier-plugin-java
        string? workingDirectory = null;
        if (entry.RequiresNode)
        {
            workingDirectory = _processRunner.GetNpmGlobalRoot();

            // Debug: if we couldn't find npm global root, report it
            if (string.IsNullOrEmpty(workingDirectory))
            {
                return new FormatResult(false,
                    $"Could not find npm global directory.\n\n" +
                    $"Make sure Node.js and npm are installed and in PATH.\n" +
                    $"Run 'npm root -g' in terminal to verify.");
            }
        }

        // Get settings and build args with them
        var settings = _configManager.GetSettingsWithDefaults(language);
        var args = BuildArgsWithSettings(language, entry.Args, settings);

        // For Node.js formatters, resolve full paths to npm binaries in the command args
        if (entry.RequiresNode && !string.IsNullOrEmpty(workingDirectory))
        {
            args = ResolveNpmBinaryPaths(args, workingDirectory);
        }

        // Use temp file approach for formatters that don't support stdin
        ProcessResult result;
        if (entry.UsesTempFile)
        {
            result = await _processRunner.RunWithTempFileAsync(command, args, code, entry.TempFileExtension, workingDirectory, cancellationToken);
        }
        else
        {
            result = await _processRunner.RunAsync(command, args, code, workingDirectory, cancellationToken);
        }

        if (!result.Success)
        {
            var errorMessage = string.IsNullOrWhiteSpace(result.Error)
                ? "Unknown formatting error occurred."
                : result.Error;

            // Add debug info for troubleshooting
            if (entry.RequiresNode)
            {
                errorMessage += $"\n\nDebug Info:\nWorking Directory: {workingDirectory}";
            }

            // Add debug info if binary not found
            if (errorMessage.Contains("cannot find the file"))
            {
                errorMessage += $"\n\n{GetDebugInfo()}";
            }

            return new FormatResult(false, $"Formatting Error\n\n{errorMessage}");
        }

        return new FormatResult(true, result.Output);
    }

    /// <summary>
    /// Checks if required npm packages are installed for the given language.
    /// Returns a friendly error message with installation instructions if missing.
    /// </summary>
    private FormatResult CheckRequiredNpmPackages(Language language)
    {
        var npmRoot = _processRunner.GetNpmGlobalRoot();
        if (string.IsNullOrEmpty(npmRoot))
        {
            return new FormatResult(false,
                "Could not find npm global directory.\n\n" +
                "Make sure Node.js and npm are installed.\n" +
                "Run 'npm root -g' in terminal to verify.");
        }

        var nodeModules = Path.Combine(npmRoot, "node_modules");

        return language switch
        {
            Language.Java => CheckJavaPackages(nodeModules),
            Language.Sql => CheckSqlPackages(nodeModules),
            _ => new FormatResult(true, string.Empty)
        };
    }

    private static FormatResult CheckJavaPackages(string nodeModules)
    {
        var prettierPath = Path.Combine(nodeModules, "prettier");
        var pluginPath = Path.Combine(nodeModules, "prettier-plugin-java");

        if (!Directory.Exists(prettierPath))
        {
            return new FormatResult(false,
                "Prettier Not Installed\n\n" +
                "Java formatting requires Prettier to be installed globally.\n\n" +
                "Install with:\n" +
                "  npm install -g prettier prettier-plugin-java\n\n" +
                "Then restart DevToys.");
        }

        if (!Directory.Exists(pluginPath))
        {
            return new FormatResult(false,
                "Java Plugin Not Installed\n\n" +
                "Java formatting requires the prettier-plugin-java package.\n\n" +
                "Install with:\n" +
                "  npm install -g prettier-plugin-java\n\n" +
                "Then restart DevToys.");
        }

        return new FormatResult(true, string.Empty);
    }

    private static FormatResult CheckSqlPackages(string nodeModules)
    {
        var sqlFormatterPath = Path.Combine(nodeModules, "sql-formatter");

        if (!Directory.Exists(sqlFormatterPath))
        {
            return new FormatResult(false,
                "SQL Formatter Not Installed\n\n" +
                "SQL formatting requires sql-formatter to be installed globally.\n\n" +
                "Install with:\n" +
                "  npm install -g sql-formatter\n\n" +
                "Then restart DevToys.");
        }

        return new FormatResult(true, string.Empty);
    }

    /// <summary>
    /// Builds command arguments with formatter-specific settings
    /// </summary>
    private static string[] BuildArgsWithSettings(Language language, string[] baseArgs, Dictionary<string, object> settings)
    {
        if (settings.Count == 0)
            return baseArgs;

        return language switch
        {
            Language.Python => BuildRuffArgs(baseArgs, settings),
            Language.JavaScript or Language.TypeScript => BuildDprintArgs(baseArgs, settings),
            Language.Json => BuildDprintArgs(baseArgs, settings),
            Language.Markdown => BuildDprintArgs(baseArgs, settings),
            Language.Toml => BuildDprintArgs(baseArgs, settings),
            Language.Css or Language.Scss or Language.Less => BuildDprintArgs(baseArgs, settings),
            Language.Html or Language.Vue or Language.Svelte or Language.Astro => BuildDprintArgs(baseArgs, settings),
            Language.Yaml => BuildDprintArgs(baseArgs, settings),
            Language.GraphQL => BuildDprintArgs(baseArgs, settings),
            Language.Dockerfile => BuildDprintArgs(baseArgs, settings),
            Language.Java => BuildPrettierArgs(baseArgs, settings),
            Language.Sql => BuildSqlFormatterArgs(baseArgs, settings),
            Language.C or Language.Cpp => BuildClangFormatArgs(baseArgs, settings),
            Language.Go => BuildGofumptArgs(baseArgs, settings),
            Language.Shell => BuildShfmtArgs(baseArgs, settings),
            Language.Lua => baseArgs,
            Language.R => baseArgs,
            Language.Delphi => baseArgs,
            Language.CSharpFormatted => baseArgs,
            Language.Assembly => baseArgs,
            Language.ObjectiveC => baseArgs,
            Language.Kotlin => baseArgs,
            Language.Haskell => baseArgs,
            Language.Perl => baseArgs,
            Language.Php => baseArgs,
            Language.Matlab => baseArgs,
            Language.Ruby => baseArgs,
            _ => baseArgs
        };
    }

    /// <summary>
    /// Build args for Ruff (Python) using --config for TOML-style overrides
    /// Format: --config "format.key=value" or --line-length for direct args
    /// </summary>
    private static string[] BuildRuffArgs(string[] baseArgs, Dictionary<string, object> settings)
    {
        var result = new List<string>(baseArgs);

        foreach (var (key, value) in settings)
        {
            var argValue = value switch
            {
                bool b => b.ToString().ToLowerInvariant(),
                string s => $"\"{s}\"",  // Quote string values for TOML
                _ => value.ToString()
            };

            // line-length is a direct CLI arg, others use --config format.key=value
            if (key == "line-length")
            {
                result.Add($"--line-length={value}");
            }
            else
            {
                // Use TOML config override format: --config "format.key=value"
                result.Add("--config");
                result.Add($"format.{key}={argValue}");
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Build args for dprint. Note: dprint doesn't support inline config via CLI args.
    /// It requires a config file with -c flag. Settings are stored but not applied
    /// until we implement temp config file generation.
    /// </summary>
    private static string[] BuildDprintArgs(string[] baseArgs, Dictionary<string, object> settings)
    {
        // dprint doesn't support inline configuration - it requires a config file
        // For now, just return base args. Settings are preserved in config for future use.
        // TODO: Implement temp config file generation for dprint settings
        return baseArgs;
    }

    /// <summary>
    /// Build args for Prettier (Java): --print-width=80 --tab-width=4
    /// Prettier args are injected into the PowerShell command
    /// </summary>
    private static string[] BuildPrettierArgs(string[] baseArgs, Dictionary<string, object> settings)
    {
        var result = new List<string>();

        foreach (var arg in baseArgs)
        {
            // Find the PowerShell -Command argument and append prettier options
            if (arg.Contains("& prettier") || arg.Contains("& '"))
            {
                var settingsArgs = new List<string>();
                foreach (var (key, value) in settings)
                {
                    // Convert camelCase to kebab-case for prettier CLI
                    var cliKey = ToKebabCase(key);
                    if (value is bool b)
                    {
                        if (b) settingsArgs.Add($"--{cliKey}");
                        else settingsArgs.Add($"--no-{cliKey}");
                    }
                    else
                    {
                        settingsArgs.Add($"--{cliKey}={value}");
                    }
                }

                // Append settings to the command
                var modifiedArg = arg + " " + string.Join(" ", settingsArgs);
                result.Add(modifiedArg);
            }
            else
            {
                result.Add(arg);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Build args for sql-formatter. Language setting modifies the -l flag.
    /// Other settings require JSON config which is complex in PowerShell.
    /// TODO: Implement temp config file for full settings support.
    /// </summary>
    private static string[] BuildSqlFormatterArgs(string[] baseArgs, Dictionary<string, object> settings)
    {
        var result = new List<string>();

        // Only language can be easily passed via CLI
        string? languageSetting = null;
        if (settings.TryGetValue("language", out var lang) && lang is string langStr)
            languageSetting = langStr;

        foreach (var arg in baseArgs)
        {
            // Find the PowerShell -Command argument and modify sql-formatter options
            if (arg.Contains("& sql-formatter") || arg.Contains("& '"))
            {
                var modifiedArg = arg;

                // Replace language in existing command if specified
                if (!string.IsNullOrEmpty(languageSetting))
                {
                    modifiedArg = System.Text.RegularExpressions.Regex.Replace(
                        modifiedArg,
                        @"--language\s+\w+",
                        $"--language {languageSetting}");
                }

                result.Add(modifiedArg);
            }
            else
            {
                result.Add(arg);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// Build args for clang-format (C/C++): --style=LLVM
    /// </summary>
    private static string[] BuildClangFormatArgs(string[] baseArgs, Dictionary<string, object> settings)
    {
        var result = new List<string>(baseArgs);

        if (settings.TryGetValue("style", out var style) && style is string styleStr)
        {
            result.Add($"--style={styleStr}");
        }

        return result.ToArray();
    }

    /// <summary>
    /// Build args for gofumpt (Go): -extra flag
    /// </summary>
    private static string[] BuildGofumptArgs(string[] baseArgs, Dictionary<string, object> settings)
    {
        var result = new List<string>(baseArgs);

        if (settings.TryGetValue("extra", out var extra) && extra is bool extraBool && extraBool)
        {
            result.Add("-extra");
        }

        return result.ToArray();
    }

    /// <summary>
    /// Build args for shfmt (Shell/Bash): -i, -bn, -ci, -sr, -kp, -fn flags
    /// </summary>
    private static string[] BuildShfmtArgs(string[] baseArgs, Dictionary<string, object> settings)
    {
        var result = new List<string>(baseArgs);

        // Indent width: -i N (0 for tabs)
        if (settings.TryGetValue("indent", out var indent))
        {
            var indentVal = indent switch
            {
                int i => i,
                long l => (int)l,
                _ => 2
            };
            result.Add("-i");
            result.Add(indentVal.ToString());
        }

        // Binary operators at start of line: -bn
        if (settings.TryGetValue("binaryNextLine", out var bn) && bn is bool bnBool && bnBool)
        {
            result.Add("-bn");
        }

        // Case indent: -ci
        if (settings.TryGetValue("caseIndent", out var ci) && ci is bool ciBool && ciBool)
        {
            result.Add("-ci");
        }

        // Space after redirects: -sr
        if (settings.TryGetValue("spaceRedirects", out var sr) && sr is bool srBool && srBool)
        {
            result.Add("-sr");
        }

        // Keep padding: -kp
        if (settings.TryGetValue("keepPadding", out var kp) && kp is bool kpBool && kpBool)
        {
            result.Add("-kp");
        }

        // Function opening brace on next line: -fn
        if (settings.TryGetValue("funcNextLine", out var fn) && fn is bool fnBool && fnBool)
        {
            result.Add("-fn");
        }

        return result.ToArray();
    }

    /// <summary>
    /// Convert camelCase to kebab-case (e.g., printWidth -> print-width)
    /// </summary>
    private static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new System.Text.StringBuilder();
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (char.IsUpper(c))
            {
                if (i > 0) result.Append('-');
                result.Append(char.ToLowerInvariant(c));
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Resolves npm binary names in args to full paths.
    /// This is needed because DevToys doesn't have npm in PATH.
    /// </summary>
    private static string[] ResolveNpmBinaryPaths(string[] args, string npmGlobalRoot)
    {
        // Known npm binaries we might need to resolve
        string[] npmBinaries = ["prettier", "sql-formatter"];

        var resolvedArgs = new string[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            // Check if this arg contains a command that needs resolution
            // e.g., "& prettier --plugin=..." or just "prettier"
            foreach (var binary in npmBinaries)
            {
                // Look for the binary name as a standalone word or after "& "
                var cmdPath = Path.Combine(npmGlobalRoot, $"{binary}.cmd");
                if (!File.Exists(cmdPath))
                    continue;

                // Replace "& prettier" with "& 'C:\path\prettier.cmd'"
                if (arg.Contains($"& {binary}"))
                {
                    arg = arg.Replace($"& {binary}", $"& '{cmdPath}'");
                    break;
                }
            }

            resolvedArgs[i] = arg;
        }

        return resolvedArgs;
    }

    private string ResolveCommand(string command)
    {
        // Check for custom path in config
        var customPath = _configManager.GetCustomPath(command);
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
            return customPath;

        // Check for bundled binary in all search paths
        foreach (var searchPath in _binarySearchPaths)
        {
            var bundledPath = Path.Combine(searchPath, $"{command}.exe");
            if (File.Exists(bundledPath))
                return bundledPath;
        }

        // Fall back to PATH lookup
        return command;
    }

    // For debugging - shows where we're looking for binaries
    public string GetDebugInfo()
    {
        var assemblyLocation = typeof(FormatterService).Assembly.Location;
        var lines = new List<string>
        {
            $"Assembly: {assemblyLocation}",
            "Search paths:"
        };
        foreach (var path in _binarySearchPaths)
        {
            var ruffPath = Path.Combine(path, "ruff.exe");
            var exists = File.Exists(ruffPath);
            lines.Add($"  {path} -> ruff.exe exists: {exists}");
        }
        return string.Join("\n", lines);
    }
}
