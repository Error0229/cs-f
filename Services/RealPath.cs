using System.Runtime.InteropServices;
using System.Text;

namespace CodeFormatter.Services;

/// <summary>
/// The path of a file as a process outside ours will find it.
///
/// DevToys from the Store is an MSIX app: Windows shows it a virtual %LOCALAPPDATA%, so the
/// extension's own files appear under AppData\Local\DevToys-preview while they really live under
/// AppData\Local\Packages\...\LocalCache\Local. Starting a formatter by such a path works, because
/// process creation goes through the same redirection, but the formatter itself cannot open a file
/// by it. Opening the file here and asking for the handle's final path gives the real location.
/// </summary>
public static class RealPath
{
    public static string Of(string path)
    {
        try
        {
            using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var buffer = new StringBuilder(1024);
            var length = GetFinalPathNameByHandleW(handle, buffer, buffer.Capacity, 0);
            if (length == 0 || length > buffer.Capacity)
                return path;

            var real = buffer.ToString(0, length);
            return real.StartsWith(@"\\?\UNC\", StringComparison.Ordinal) ? @"\\" + real[8..]
                : real.StartsWith(@"\\?\", StringComparison.Ordinal) ? real[4..]
                : real;
        }
        catch
        {
            return path;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetFinalPathNameByHandleW(SafeHandle hFile, StringBuilder lpszFilePath, int cchFilePath, int dwFlags);
}
