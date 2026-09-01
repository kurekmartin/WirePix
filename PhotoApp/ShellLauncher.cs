using System.Diagnostics;

namespace PhotoApp;

internal static class ShellLauncher
{
    public static void Open(string target)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        });
    }
}