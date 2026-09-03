using System;
using System.IO;

namespace WirePix.Core.Storage;

public sealed record ApplicationDataPaths(
    string Main,
    string Temp,
    string Logs,
    string Profiles,
    string Data,
    string CrashReports)
{
    public static ApplicationDataPaths CreateDefault(string applicationName)
    {
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string main = Path.Combine(roaming, applicationName);
        return new ApplicationDataPaths(
            main,
            Path.Combine(Path.GetTempPath(), applicationName),
            Path.Combine(main, "Logs"),
            Path.Combine(main, "Profiles"),
            Path.Combine(main, "Data"),
            Path.Combine(main, "Crash Reports"));
    }
}