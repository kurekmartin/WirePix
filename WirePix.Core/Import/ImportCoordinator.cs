using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WirePix.Core.Files;
using WirePix.Core.Import.Contracts;
using WirePix.Core.Models.Settings;
using WirePix.Core.Naming.Templates;
using WirePix.Core.Imaging;
using WirePix.Core.Logging;

namespace WirePix.Core.Import;

public sealed class ImportCoordinator(string tempFolder, string logFolder = null)
{
    private const int MaxAttempts = 5;
    private readonly string _tempFolder = tempFolder ?? throw new ArgumentNullException(paramName: nameof(tempFolder));
    private readonly FileLogger _logger = logFolder == null ? null : new FileLogger(folder: logFolder);
    private readonly ThumbnailGenerator _thumbnails = new();

    public static Task<ImportPlan> CreatePlanAsync(
        IImportDevice device,
        DownloadSettings settings,
        IProgress<ImportProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(function: () => CreatePlan(device: device, settings: settings, progress: progress, token: cancellationToken), cancellationToken: cancellationToken);
    }

    private static ImportPlan CreatePlan(
        IImportDevice device,
        DownloadSettings settings,
        IProgress<ImportProgress> progress,
        CancellationToken token)
    {
        var files = new List<ImportFile>();
        progress?.Report(value: new ImportProgress(stage: ImportStage.Searching, completed: 0, total: 0));
        device.Connect();
        try
        {
            foreach (ImportDrive drive in device.GetDrives() ?? [])
            {
                FindMediaFiles(device: device, path: drive.RootPath, result: files, progress: progress, token: token);
            }
        }
        finally
        {
            device.Disconnect();
        }

        IEnumerable<ImportFile> selected = files;
        if (settings?.Date != null && settings.Date.Start != default)
        {
            progress?.Report(value: new ImportProgress(stage: ImportStage.Filtering, completed: 0, total: files.Count));
            DateTime end = settings.Date.End.Date.AddDays(value: 1);
            selected = files.Where(predicate: f => EffectiveDate(file: f) >= settings.Date.Start && EffectiveDate(file: f) < end);
        }

        List<ImportFile> result =
        [
            .. selected.OrderBy(keySelector: EffectiveDate)
                       .ThenBy(keySelector: f => f.Name)
        ];

        return new ImportPlan
        {
            Files = result,
            TotalBytes = result.Sum(selector: x => x.Length),
            Date = settings?.Date
        };
    }

    private static void FindMediaFiles(
        IImportDevice device,
        string path,
        List<ImportFile> result,
        IProgress<ImportProgress> progress,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        List<string> directories = [.. device.EnumerateDirectories(path: path) ?? []];
        string dcim = directories.FirstOrDefault(predicate: x =>
            string.Equals(a: Path.GetFileName(path: x.TrimEnd('\\', '/')), b: "DCIM", comparisonType: StringComparison.OrdinalIgnoreCase));

        if (dcim != null)
        {
            path = dcim;
        }
        else
        {
            foreach (string child in directories)
            {
                string name = Path.GetFileName(path: child.TrimEnd('\\', '/'));
                if (!string.IsNullOrEmpty(value: name) && !name.StartsWith(value: '.'))
                {
                    FindMediaFiles(device: device, path: child, result: result, progress: progress, token: token);
                }
            }

            return;
        }

        EnumerateDirectory(device: device, path: path, result: result, progress: progress, token: token);
    }

    private static void EnumerateDirectory(
        IImportDevice device,
        string path,
        List<ImportFile> result,
        IProgress<ImportProgress> progress,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        foreach (ImportFile file in device.EnumerateFiles(path: path) ?? [])
        {
            result.Add(item: file);
            progress?.Report(value: new ImportProgress(stage: ImportStage.Searching, completed: result.Count, total: 0, currentFile: file.FullName));
        }

        foreach (string child in device.EnumerateDirectories(path: path) ?? [])
        {
            string name = Path.GetFileName(path: child.TrimEnd('\\', '/'));
            if (!string.IsNullOrEmpty(value: name) && !name.StartsWith(value: '.'))
            {
                EnumerateDirectory(device: device, path: child, result: result, progress: progress, token: token);
            }
        }
    }

    public Task<ImportResult> ExecuteAsync(
        IImportDevice device,
        ImportPlan plan,
        DownloadSettings settings,
        NamingContext naming,
        IProgress<ImportProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(function: () => Execute(device: device, plan: plan, settings: settings, naming: naming, progress: progress, token: cancellationToken), cancellationToken: cancellationToken);
    }

    private ImportResult Execute(
        IImportDevice device,
        ImportPlan plan,
        DownloadSettings settings,
        NamingContext naming,
        IProgress<ImportProgress> progress,
        CancellationToken token)
    {
        var result = new ImportResult { FilesTotal = plan?.Files?.Count ?? 0 };
        if (plan?.Files == null || plan.Files.Count == 0)
        {
            return result;
        }

        Directory.CreateDirectory(path: _tempFolder);
        _logger?.Start();
        var toDelete = new List<string>();
        device.Connect();
        try
        {
            for (var index = 0; index < plan.Files.Count; index++)
            {
                token.ThrowIfCancellationRequested();
                ImportFile file = plan.Files[index: index];
                progress?.Report(value: new ImportProgress(stage: ImportStage.Downloading, completed: index, total: plan.Files.Count, currentFile: file.FullName));
                string temp = Path.Combine(path1: _tempFolder, path2: Guid.NewGuid().ToString(format: "N"));
                try
                {
                    byte[] originalHash = null;
                    if (settings.CheckFiles)
                    {
                        using Stream stream = device.OpenRead(file: file);
                        originalHash = FileOperations.Hash(stream: stream);
                    }

                    var downloaded = false;
                    for (var attempt = 0; attempt < MaxAttempts && !downloaded; attempt++)
                    {
                        device.Download(file: file, destination: temp);
                        downloaded = !settings.CheckFiles || FileOperations.Verify(expected: originalHash, path: temp);
                    }

                    if (!downloaded)
                    {
                        throw new IOException(message: "Downloaded file failed hash verification.");
                    }

                    originalHash ??= FileOperations.Hash(stream: File.OpenRead(path: temp));

                    var context = new NamingContext
                    {
                        File = file,
                        DeviceName = naming?.DeviceName ?? device.Name,
                        Manufacturer = naming?.Manufacturer ?? device.Manufacturer,
                        Culture = naming?.Culture
                    };

                    string relativeFolder = NameTemplate.EvaluateFolders(folders: settings.Paths.FolderTags, context: context);
                    string relativeName = NameTemplate.Evaluate(tags: settings.Paths.FileTags, context: context) + Path.GetExtension(path: file.Name);
                    string relativePath = Path.Combine(path1: relativeFolder ?? string.Empty, path2: relativeName);
                    if (settings.Thumbnail)
                    {
                        progress?.Report(value: new ImportProgress(stage: ImportStage.GeneratingThumbnail, completed: index, total: plan.Files.Count, currentFile: file.FullName));
                    }

                    if (settings.Thumbnail && ThumbnailGenerator.IsImage(path: temp))
                    {
                        string thumbnailPath = Path.Combine(path1: settings.Paths.Thumbnail ?? string.Empty, path2: relativePath);
                        thumbnailPath = Path.Combine(path1: Path.GetDirectoryName(path: thumbnailPath) ?? string.Empty, path2: Path.GetFileNameWithoutExtension(path: thumbnailPath) + "(" + Path.GetExtension(path: thumbnailPath).TrimStart(trimChar: '.') + ").jpg");
                        ThumbnailGenerator.Generate(source: temp, output: thumbnailPath, settings: settings.ThumbnailSettings, hash: Convert.ToHexStringLower(inArray: originalHash));
                    }

                    if (!SaveToRoots(settings: settings, relativePath: relativePath, temp: temp, hash: originalHash, original: file.FullName))
                    {
                        throw new IOException(message: "Could not save file.");
                    }

                    result.FilesDone++;
                    if (settings.DeleteFiles)
                    {
                        toDelete.Add(item: file.FullName);
                    }
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    _logger?.Add(message: ex.ToString(), type: LogType.Error, function: nameof(Execute));
                }
                finally
                {
                    if (File.Exists(path: temp))
                    {
                        File.Delete(path: temp);
                    }
                }
            }

            foreach (string source in toDelete)
            {
                progress?.Report(value: new ImportProgress(stage: ImportStage.Deleting, completed: result.Deleted, total: toDelete.Count, currentFile: source));
                device.Delete(fullName: source);
                result.Deleted++;
            }
        }
        finally
        {
            device.Disconnect();
            _logger?.Stop();
        }

        progress?.Report(value: new ImportProgress(stage: ImportStage.Completed, completed: result.FilesDone, total: result.FilesTotal));
        return result;
    }

    private static bool SaveToRoots(DownloadSettings settings, string relativePath, string temp, byte[] hash, string original)
    {
        foreach (string root in new[] { settings.Paths.Root, settings.Paths.Backup })
        {
            if (string.IsNullOrEmpty(value: root))
            {
                return true;
            }

            string destination = Path.Combine(path1: root, path2: relativePath);
            if (File.Exists(path: destination))
            {
                destination = FileOperations.UniqueName(path: destination, sourcePath: temp);
            }

            Directory.CreateDirectory(path: Path.GetDirectoryName(path: destination) ?? root);
            var copied = false;
            for (var attempt = 0; attempt < MaxAttempts && !copied; attempt++)
            {
                File.Copy(sourceFileName: temp, destFileName: destination, overwrite: true);
                copied = !settings.CheckFiles || FileOperations.Verify(expected: hash, path: destination);
            }

            if (!copied)
            {
                return false;
            }
        }

        return true;
    }

    private static DateTime EffectiveDate(ImportFile file)
    {
        return file.CreationTime != default ? file.CreationTime : file.DateAuthored != default ? file.DateAuthored : file.LastWriteTime;
    }
}