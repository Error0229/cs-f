using System.Diagnostics;

namespace CodeFormatter.Services;

public record ProcessResult(bool Success, string Output, string Error);

public class ProcessRunner
{
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30); // Increased for large files
    private string? _npmGlobalRoot;

    public async Task<ProcessResult> RunAsync(string command, string[] args, string input, string? workingDirectory = null, CancellationToken cancellationToken = default)
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

        if (!string.IsNullOrEmpty(workingDirectory))
            process.StartInfo.WorkingDirectory = workingDirectory;

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

    /// <summary>
    /// Gets the global npm root directory where global packages are installed.
    /// This is needed for prettier-plugin-java to be found.
    /// </summary>
    public string? GetNpmGlobalRoot()
    {
        if (_npmGlobalRoot != null)
            return _npmGlobalRoot;

        // Try running npm root -g first
        _npmGlobalRoot = TryGetNpmRootFromCommand();
        if (_npmGlobalRoot != null)
            return _npmGlobalRoot;

        // Fall back to checking common locations
        _npmGlobalRoot = TryFindNpmGlobalFromCommonPaths();
        return _npmGlobalRoot;
    }

    private string? TryGetNpmRootFromCommand()
    {
        // Try different npm executable names/paths
        string[] npmCandidates = ["npm", "npm.cmd"];

        foreach (var npm in npmCandidates)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = npm,
                    Arguments = "root -g",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(5000);

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output) && Directory.Exists(output))
                {
                    // Get parent directory (npm root -g returns node_modules, we need parent)
                    return Path.GetDirectoryName(output);
                }
            }
            catch
            {
                // Try next candidate
            }
        }

        return null;
    }

    private string? TryFindNpmGlobalFromCommonPaths()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Common locations where global npm packages might be installed
        string[] commonPaths =
        [
            // Scoop with nvm (user's setup)
            Path.Combine(userProfile, "scoop", "apps", "nvm", "current", "nodejs", "nodejs"),
            // Standard nvm-windows
            Path.Combine(appData, "nvm"),
            // Standard npm global on Windows
            Path.Combine(appData, "npm"),
            // Chocolatey nodejs
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs"),
            // fnm (Fast Node Manager)
            Path.Combine(localAppData, "fnm_multishells"),
        ];

        foreach (var basePath in commonPaths)
        {
            // Check if this path has a node_modules with prettier-plugin-java
            var nodeModulesPath = Path.Combine(basePath, "node_modules", "prettier-plugin-java");
            if (Directory.Exists(nodeModulesPath))
            {
                return basePath;
            }

            // Also check parent directories in case of nvm version folders
            if (Directory.Exists(basePath))
            {
                try
                {
                    foreach (var dir in Directory.GetDirectories(basePath))
                    {
                        nodeModulesPath = Path.Combine(dir, "node_modules", "prettier-plugin-java");
                        if (Directory.Exists(nodeModulesPath))
                        {
                            return dir;
                        }
                    }
                }
                catch
                {
                    // Ignore permission errors
                }
            }
        }

        return null;
    }
}
