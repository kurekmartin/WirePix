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
    public sealed class ImportCoordinator
    {
        private const int MaxAttempts = 5;
        private readonly string _tempFolder;
        private readonly FileLogger _logger;
        private readonly ThumbnailGenerator _thumbnails = new ThumbnailGenerator();

        public ImportCoordinator(string tempFolder, string logFolder = null)
        {
            _tempFolder = tempFolder ?? throw new ArgumentNullException(nameof(tempFolder));
            _logger = logFolder == null ? null : new FileLogger(logFolder);
        }

        public Task<ImportPlan> CreatePlanAsync(IImportDevice device, DownloadSettings settings, IProgress<ImportProgress> progress = null, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => CreatePlan(device, settings, progress, cancellationToken), cancellationToken);
        }

        private ImportPlan CreatePlan(IImportDevice device, DownloadSettings settings, IProgress<ImportProgress> progress, CancellationToken token)
        {
            var files = new List<ImportFile>();
            progress?.Report(new ImportProgress(ImportStage.Searching, 0, 0));
            device.Connect();
            try
            {
                foreach (var drive in device.GetDrives() ?? Array.Empty<ImportDrive>())
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
                var end = settings.Date.End.Date.AddDays(1);
                selected = files.Where(f => EffectiveDate(f) >= settings.Date.Start && EffectiveDate(f) < end);
            }

            var result = selected.OrderBy(f => EffectiveDate(f)).ThenBy(f => f.Name).ToList();
            return new ImportPlan { Files = result, TotalBytes = result.Sum(x => x.Length), Date = settings?.Date };
        }

        private void FindMediaFiles(IImportDevice device, string path, List<ImportFile> result, IProgress<ImportProgress> progress, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var directories = (device.EnumerateDirectories(path) ?? Enumerable.Empty<string>()).ToList();
            var dcim = directories.FirstOrDefault(x => string.Equals(Path.GetFileName(x.TrimEnd('\\', '/')), "DCIM", StringComparison.OrdinalIgnoreCase));
            if (dcim != null)
            {
                path = dcim;
            }
            else
            {
                foreach (var child in directories)
                {
                    var name = Path.GetFileName(child.TrimEnd('\\', '/'));
                    if (!string.IsNullOrEmpty(name) && !name.StartsWith(".", StringComparison.Ordinal))
                    {
                        FindMediaFiles(device, child, result, progress, token);
                    }
                }

                return;
            }

            EnumerateDirectory(device, path, result, progress, token);
        }

        private void EnumerateDirectory(IImportDevice device, string path, List<ImportFile> result, IProgress<ImportProgress> progress, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            foreach (var file in device.EnumerateFiles(path) ?? Enumerable.Empty<ImportFile>())
            {
                result.Add(file);
                progress?.Report(new ImportProgress(ImportStage.Searching, result.Count, 0, file.FullName));
            }

            foreach (var child in device.EnumerateDirectories(path) ?? Enumerable.Empty<string>())
            {
                var name = Path.GetFileName(child.TrimEnd('\\', '/'));
                if (!string.IsNullOrEmpty(name) && !name.StartsWith(".", StringComparison.Ordinal))
                {
                    EnumerateDirectory(device, child, result, progress, token);
                }
            }
        }

        public Task<ImportResult> ExecuteAsync(IImportDevice device, ImportPlan plan, DownloadSettings settings, NamingContext naming, IProgress<ImportProgress> progress = null, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => Execute(device, plan, settings, naming, progress, cancellationToken), cancellationToken);
        }

        private ImportResult Execute(IImportDevice device, ImportPlan plan, DownloadSettings settings, NamingContext naming, IProgress<ImportProgress> progress, CancellationToken token)
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
                    var file = plan.Files[index];
                    progress?.Report(new ImportProgress(ImportStage.Downloading, index, plan.Files.Count, file.FullName));
                    var temp = Path.Combine(_tempFolder, Guid.NewGuid().ToString("N"));
                    try
                    {
                        byte[] originalHash = null;
                        if (settings.CheckFiles)
                        {
                            using (var stream = device.OpenRead(file))
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

                        if (originalHash == null)
                        {
                            originalHash = FileOperations.Hash(File.OpenRead(temp));
                        }

                        var context = new NamingContext { File = file, DeviceName = naming?.DeviceName ?? device.Name, Manufacturer = naming?.Manufacturer ?? device.Manufacturer, Culture = naming?.Culture };
                        var relativeFolder = NameTemplate.EvaluateFolders(settings.Paths.FolderTags, context);
                        var relativeName = NameTemplate.Evaluate(settings.Paths.FileTags, context) + Path.GetExtension(file.Name);
                        var relativePath = Path.Combine(relativeFolder ?? string.Empty, relativeName);
                        if (settings.Thumbnail)
                        {
                            progress?.Report(new ImportProgress(ImportStage.GeneratingThumbnail, index, plan.Files.Count, file.FullName));
                        }

                        if (settings.Thumbnail && ThumbnailGenerator.IsImage(temp))
                        {
                            var thumbnailPath = Path.Combine(settings.Paths.Thumbnail ?? string.Empty, relativePath);
                            thumbnailPath = Path.Combine(Path.GetDirectoryName(thumbnailPath) ?? string.Empty, Path.GetFileNameWithoutExtension(thumbnailPath) + "(" + Path.GetExtension(thumbnailPath).TrimStart('.') + ").jpg");
                            _thumbnails.Generate(temp, thumbnailPath, settings.ThumbnailSettings, BitConverter.ToString(originalHash).Replace("-", string.Empty).ToLowerInvariant());
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
                        _logger?.Add(ex.ToString(), LogType.ERROR, nameof(Execute));
                    }
                    finally
                    {
                        if (File.Exists(temp))
                        {
                            File.Delete(temp);
                        }
                    }
                }

                foreach (var source in toDelete)
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
            foreach (var root in new[] { settings.Paths.Root, settings.Paths.Backup })
            {
                if (string.IsNullOrEmpty(root))
                {
                    return true;
                }

                var destination = Path.Combine(root, relativePath);
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