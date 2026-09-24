using System.Diagnostics;
using System.Text;

namespace CodeFormatter.Services;

/// <summary>
/// What the process did. Whether that counts as success is the formatter spec's call, not ours.
/// ExitCode is null when the process never ran to completion (could not start, timed out).
/// </summary>
public record ProcessResult(int? ExitCode, string Output, string Error);

public class ProcessRunner
{
    // Formatters speak UTF-8. The default is the system code page, which mangles non-Latin text.
    // No BOM: a preamble on stdin would be fed to the formatter as source text.
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30); // Increased for large files

    /// <param name="input">Text for stdin, or null when the tool works on a file.</param>
    public async Task<ProcessResult> RunAsync(
        string command,
        IEnumerable<string> args,
        string? input,
        string workingDirectory,
        IReadOnlyDictionary<string, string>? environment = null,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = command,
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Utf8NoBom,
            StandardOutputEncoding = Utf8NoBom,
            StandardErrorEncoding = Utf8NoBom,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            process.StartInfo.ArgumentList.Add(arg);

        foreach (var (name, value) in environment ?? new Dictionary<string, string>())
            process.StartInfo.Environment[name] = value;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        try
        {
            process.Start();

            // Start draining before writing: a formatter that streams its output while we are
            // still feeding it a large input would otherwise block on a full pipe, and so would we.
            var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

            try
            {
                // Always close stdin, also for file-based tools: some prompt when it is left open.
                if (input is not null)
                    await process.StandardInput.WriteAsync(input.AsMemory(), cts.Token);
                process.StandardInput.Close();
            }
            catch (IOException)
            {
                // The tool exited without reading its stdin (bad option, crash).
                // Its exit code and stderr tell the story.
            }

            await process.WaitForExitAsync(cts.Token);

            return new ProcessResult(process.ExitCode, await outputTask, await errorTask);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            // The caller lost interest (the user kept typing): nobody reads this result
            cancellationToken.ThrowIfCancellationRequested();
            return new ProcessResult(null, "", "Formatting timed out. Code may be too large.");
        }
        catch (Exception ex)
        {
            Kill(process);
            return new ProcessResult(null, "", $"Failed to run formatter: {ex.Message}");
        }
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Never started, or already gone
        }
    }
}
