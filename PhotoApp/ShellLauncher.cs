using System.Diagnostics;

namespace PhotoApp;

internal static class ShellLauncher
{
    public static void Open(string target)
    {
        Process.Start(startInfo: new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        });
    }
}