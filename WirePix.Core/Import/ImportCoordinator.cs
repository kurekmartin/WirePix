using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WirePix.Core.Files;
using WirePix.Core.Models;
using WirePix.Core.Naming;
using WirePix.Core.Storage;
using WirePix.Core.Imaging;

namespace WirePix.Core.Import
{
    public sealed class ImportCoordinator(string tempFolder, string logFolder = null)
    {
        private const int MaxAttempts = 5;
        private readonly string _tempFolder = tempFolder ?? throw new ArgumentNullException(nameof(tempFolder));
        private readonly FileLogger _logger = logFolder == null ? null : new FileLogger(logFolder);
        private readonly ThumbnailGenerator _thumbnails = new();

        public static Task<ImportPlan> CreatePlanAsync(
            IImportDevice device,
            DownloadSettings settings,
            IProgress<ImportProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() => CreatePlan(device, settings, progress, cancellationToken), cancellationToken);
        }

        private static ImportPlan CreatePlan(
            IImportDevice device,
            DownloadSettings settings,
            IProgress<ImportProgress> progress,
            CancellationToken token)
        {
            var files = new List<ImportFile>();
            progress?.Report(new ImportProgress(ImportStage.Searching, 0, 0));
            device.Connect();
            try
            {
                foreach (ImportDrive drive in device.GetDrives() ?? [])
                    FindMediaFiles(device, drive.RootPath, files, progress, token);
            }
            finally
            {
                device.Disconnect();
            }

            IEnumerable<ImportFile> selected = files;
            if (settings?.Date != null && settings.Date.Start != default)
            {
                progress?.Report(new ImportProgress(ImportStage.Filtering, 0, files.Count));
                DateTime end = settings.Date.End.Date.AddDays(1);
                selected = files.Where(f => EffectiveDate(f) >= settings.Date.Start && EffectiveDate(f) < end);
            }

            List<ImportFile> result =
            [
                .. selected.OrderBy(EffectiveDate)
                           .ThenBy(f => f.Name)
            ];

            return new ImportPlan
            {
                Files = result,
                TotalBytes = result.Sum(x => x.Length),
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
            List<string> directories = [.. device.EnumerateDirectories(path) ?? []];
            string dcim = directories.FirstOrDefault(x =>
                string.Equals(Path.GetFileName(x.TrimEnd('\\', '/')), "DCIM", StringComparison.OrdinalIgnoreCase));

            if (dcim != null)
            {
                path = dcim;
            }
            else
            {
                foreach (string child in directories)
                {
                    string name = Path.GetFileName(child.TrimEnd('\\', '/'));
                    if (!string.IsNullOrEmpty(name) && !name.StartsWith('.'))
                    {
                        FindMediaFiles(device, child, result, progress, token);
                    }
                }

                return;
            }

            EnumerateDirectory(device, path, result, progress, token);
        }

        private static void EnumerateDirectory(
            IImportDevice device,
            string path,
            List<ImportFile> result,
            IProgress<ImportProgress> progress,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            foreach (ImportFile file in device.EnumerateFiles(path) ?? [])
            {
                result.Add(file);
                progress?.Report(new ImportProgress(ImportStage.Searching, result.Count, 0, file.FullName));
            }

            foreach (string child in device.EnumerateDirectories(path) ?? [])
            {
                string name = Path.GetFileName(child.TrimEnd('\\', '/'));
                if (!string.IsNullOrEmpty(name) && !name.StartsWith('.'))
                {
                    EnumerateDirectory(device, child, result, progress, token);
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
            return Task.Run(() => Execute(device, plan, settings, naming, progress, cancellationToken), cancellationToken);
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

            Directory.CreateDirectory(_tempFolder);
            _logger?.Start();
            var toDelete = new List<string>();
            device.Connect();
            try
            {
                for (var index = 0; index < plan.Files.Count; index++)
                {
                    token.ThrowIfCancellationRequested();
                    ImportFile file = plan.Files[index];
                    progress?.Report(new ImportProgress(ImportStage.Downloading, index, plan.Files.Count, file.FullName));
                    string temp = Path.Combine(_tempFolder, Guid.NewGuid().ToString("N"));
                    try
                    {
                        byte[] originalHash = null;
                        if (settings.CheckFiles)
                        {
                            using Stream stream = device.OpenRead(file);
                            originalHash = FileOperations.Hash(stream);
                        }

                        var downloaded = false;
                        for (var attempt = 0; attempt < MaxAttempts && !downloaded; attempt++)
                        {
                            device.Download(file, temp);
                            downloaded = !settings.CheckFiles || FileOperations.Verify(originalHash, temp);
                        }

                        if (!downloaded)
                        {
                            throw new IOException("Downloaded file failed hash verification.");
                        }

                        originalHash ??= FileOperations.Hash(File.OpenRead(temp));

                        var context = new NamingContext
                        {
                            File = file,
                            DeviceName = naming?.DeviceName ?? device.Name,
                            Manufacturer = naming?.Manufacturer ?? device.Manufacturer,
                            Culture = naming?.Culture
                        };

                        string relativeFolder = NameTemplate.EvaluateFolders(settings.Paths.FolderTags, context);
                        string relativeName = NameTemplate.Evaluate(settings.Paths.FileTags, context) + Path.GetExtension(file.Name);
                        string relativePath = Path.Combine(relativeFolder ?? string.Empty, relativeName);
                        if (settings.Thumbnail)
                        {
                            progress?.Report(new ImportProgress(ImportStage.GeneratingThumbnail, index, plan.Files.Count, file.FullName));
                        }

                        if (settings.Thumbnail && ThumbnailGenerator.IsImage(temp))
                        {
                            string thumbnailPath = Path.Combine(settings.Paths.Thumbnail ?? string.Empty, relativePath);
                            thumbnailPath = Path.Combine(Path.GetDirectoryName(thumbnailPath) ?? string.Empty, Path.GetFileNameWithoutExtension(thumbnailPath) + "(" + Path.GetExtension(thumbnailPath).TrimStart('.') + ").jpg");
                            ThumbnailGenerator.Generate(temp, thumbnailPath, settings.ThumbnailSettings, Convert.ToHexStringLower(originalHash));
                        }

                        if (!SaveToRoots(settings, relativePath, temp, originalHash, file.FullName))
                        {
                            throw new IOException("Could not save file.");
                        }

                        result.FilesDone++;
                        if (settings.DeleteFiles)
                        {
                            toDelete.Add(file.FullName);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors++;
                        _logger?.Add(ex.ToString(), LogType.Error, nameof(Execute));
                    }
                    finally
                    {
                        if (File.Exists(temp))
                        {
                            File.Delete(temp);
                        }
                    }
                }

                foreach (string source in toDelete)
                {
                    progress?.Report(new ImportProgress(ImportStage.Deleting, result.Deleted, toDelete.Count, source));
                    device.Delete(source);
                    result.Deleted++;
                }
            }
            finally
            {
                device.Disconnect();
                _logger?.Stop();
            }

            progress?.Report(new ImportProgress(ImportStage.Completed, result.FilesDone, result.FilesTotal));
            return result;
        }

        private static bool SaveToRoots(DownloadSettings settings, string relativePath, string temp, byte[] hash, string original)
        {
            foreach (string root in new[] { settings.Paths.Root, settings.Paths.Backup })
            {
                if (string.IsNullOrEmpty(root))
                {
                    return true;
                }

                string destination = Path.Combine(root, relativePath);
                if (File.Exists(destination))
                {
                    destination = FileOperations.UniqueName(destination, temp);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? root);
                var copied = false;
                for (var attempt = 0; attempt < MaxAttempts && !copied; attempt++)
                {
                    File.Copy(temp, destination, true);
                    copied = !settings.CheckFiles || FileOperations.Verify(hash, destination);
                }

                if (!copied)
                {
                    return false;
                }
            }

            return true;
        }

        private static DateTime EffectiveDate(ImportFile file) => file.CreationTime != default ? file.CreationTime : file.DateAuthored != default ? file.DateAuthored : file.LastWriteTime;
    }
}