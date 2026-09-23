using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace CodeFormatter.Services;

/// <summary>
/// The path of a file or directory as a process outside ours will find it.
///
/// DevToys from the Store is an MSIX app: Windows shows it a virtual %LOCALAPPDATA%, so the
/// extension's own files appear under AppData\Local\DevToys-preview while they really live under
/// AppData\Local\Packages\...\LocalCache\Local. Starting a formatter by such a path works, because
/// process creation goes through the same redirection, but the formatter itself cannot open a file
/// by it. Opening the path here and asking for the handle's final name gives the real location.
/// With the MSI or portable DevToys nothing is redirected and the path comes back as it went in.
/// </summary>
public static class RealPath
{
    public static string Of(string path)
    {
        try
        {
            using var handle = CreateFileW(path, 0, FileShareAll, IntPtr.Zero, OpenExisting, FileFlagBackupSemantics, IntPtr.Zero);
            if (handle.IsInvalid)
                return path;

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

    private const uint FileShareAll = 0x1 | 0x2 | 0x4;
    private const uint OpenExisting = 3;
    private const uint FileFlagBackupSemantics = 0x02000000; // needed to open a directory

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string fileName, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetFinalPathNameByHandleW(SafeFileHandle handle, StringBuilder path, int capacity, int flags);
}
