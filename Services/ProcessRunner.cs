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
