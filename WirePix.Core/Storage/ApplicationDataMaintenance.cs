using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WirePix.Core.Storage;

public static class ApplicationDataMaintenance
{
    public static void Prepare(ApplicationDataPaths paths, int maximumLogs)
    {
        foreach (string directory in Directories(paths)) Directory.CreateDirectory(directory);
        DeleteFiles(paths.Temp, file => file.Extension.Equals(".msi", System.StringComparison.OrdinalIgnoreCase));
        foreach (FileInfo file in new DirectoryInfo(paths.Logs).GetFiles().OrderByDescending(file => file.CreationTime).Skip(System.Math.Max(0, maximumLogs)))
            file.Delete();
    }

    public static void ClearTemporaryFiles(ApplicationDataPaths paths) =>
        DeleteFiles(paths.Temp, file => !file.Extension.Equals(".msi", System.StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> Directories(ApplicationDataPaths paths) =>
        [paths.Main, paths.Temp, paths.Logs, paths.Profiles, paths.Data, paths.CrashReports];

    private static void DeleteFiles(string directory, System.Func<FileInfo, bool> predicate)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (FileInfo file in new DirectoryInfo(directory).GetFiles().Where(predicate)) file.Delete();
    }
}
