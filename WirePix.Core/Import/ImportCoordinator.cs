using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using WirePix.Core.Files;
using WirePix.Core.Import.Contracts;
using WirePix.Core.Imaging;
using WirePix.Core.Logging;
using WirePix.Core.Models.Settings;
using WirePix.Core.Naming.Templates;
using WirePix.Devices.Contracts;
using WirePix.Devices.Models;

namespace WirePix.Core.Import;

public sealed class ImportCoordinator(string tempFolder, string logFolder = null)
{
    private const int MaxAttempts = 5;
    private readonly string _tempFolder = tempFolder ?? throw new ArgumentNullException(paramName: nameof(tempFolder));
    private readonly FileLogger _logger = logFolder == null ? null : new FileLogger(folder: logFolder);

    public async Task<ImportPlan> CreatePlanAsync(
        IMediaDevice device,
        DownloadSettings settings,
        IProgress<ImportProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        progress?.Report(value: new ImportProgress(stage: ImportStage.Searching, completed: 0, total: 0));
        _logger?.Start();
        IReadOnlyList<MediaItem> files;
        try
        {
            IProgress<MediaDiscoveryUpdate> discoveryProgress = _logger == null
                ? null
                : new DiscoveryProgress(logger: _logger);
            files = await device.GetMediaAsync(discoveryProgress, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _logger?.Stop();
        }

        for (var index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(value: new ImportProgress(
                stage: ImportStage.Searching,
                completed: index + 1,
                total: 0,
                currentFile: files[index].FileName));
        }

        IEnumerable<MediaItem> selected = files;
        if (settings?.Date is not null && settings.Date.Start != default)
        {
            progress?.Report(value: new ImportProgress(stage: ImportStage.Filtering, completed: 0, total: files.Count));
            DateTime end = settings.Date.End.Date.AddDays(value: 1);
            selected = files.Where(predicate: file =>
                EffectiveDate(file) >= settings.Date.Start && EffectiveDate(file) < end);
        }

        List<MediaItem> result =
        [
            .. selected.OrderBy(keySelector: EffectiveDate)
                       .ThenBy(keySelector: file => file.FileName)
        ];

        return new ImportPlan
        {
            Files = result,
            TotalBytes = result.Sum(selector: item => item.Size ?? 0),
            Date = settings?.Date == null
                ? null
                : new DateRange { Start = settings.Date.Start, End = settings.Date.End }
        };
    }

    public async Task<ImportResult> ExecuteAsync(
        IMediaDevice device,
        ImportPlan plan,
        DownloadSettings settings,
        NamingContext naming,
        IProgress<ImportProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        var result = new ImportResult { FilesTotal = plan?.Files?.Count ?? 0 };
        if (plan?.Files == null || plan.Files.Count == 0)
        {
            return result;
        }

        Directory.CreateDirectory(path: _tempFolder);
        _logger?.Start();
        var toDelete = new List<MediaItem>();
        var importTimer = Stopwatch.StartNew();
        var bytesCompleted = 0L;
        TimeSpan estimatedRemaining = TimeSpan.Zero;
        bool useByteEstimate = plan.TotalBytes > 0 && plan.Files.All(predicate: file => file.Size.HasValue);
        try
        {
            for (var index = 0; index < plan.Files.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                MediaItem file = plan.Files[index: index];
                progress?.Report(value: new ImportProgress(
                    stage: ImportStage.Downloading,
                    completed: index,
                    total: plan.Files.Count,
                    currentFile: file.FileName,
                    bytesCompleted: bytesCompleted,
                    bytesTotal: plan.TotalBytes,
                    estimatedRemaining: estimatedRemaining));
                string temp = Path.Combine(path1: _tempFolder, path2: Guid.NewGuid().ToString(format: "N"));
                long processedBytes = file.Size ?? 0;
                try
                {
                    byte[] originalHash = null;
                    if (settings.CheckFiles)
                    {
                        originalHash = await HashDeviceItemAsync(device, file, cancellationToken).ConfigureAwait(false);
                    }

                    var downloaded = false;
                    for (var attempt = 0; attempt < MaxAttempts && !downloaded; attempt++)
                    {
                        await DownloadToFileAsync(device, file, temp, cancellationToken).ConfigureAwait(false);
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
                        LocalFilePath = temp,
                        DeviceName = string.IsNullOrEmpty(value: naming?.DeviceName)
                            ? device.DisplayName
                            : naming.DeviceName,
                        Manufacturer = string.IsNullOrEmpty(value: naming?.Manufacturer)
                            ? device.Manufacturer ?? string.Empty
                            : naming.Manufacturer,
                        Culture = naming?.Culture,
                        TagLanguage = naming?.TagLanguage,
                        UseTagLanguage = naming?.UseTagLanguage ?? false
                    };

                    string relativeFolder = NameTemplate.EvaluateFolders(folders: settings.Paths.FolderTags, context: context);
                    string relativeName = NameTemplate.Evaluate(tags: settings.Paths.FileTags, context: context) + Path.GetExtension(path: file.FileName);
                    string relativePath = Path.Combine(path1: relativeFolder ?? string.Empty, path2: relativeName);
                    if (settings.Thumbnail)
                    {
                        progress?.Report(value: new ImportProgress(
                            stage: ImportStage.GeneratingThumbnail,
                            completed: index,
                            total: plan.Files.Count,
                            currentFile: file.FileName,
                            bytesCompleted: bytesCompleted,
                            bytesTotal: plan.TotalBytes,
                            estimatedRemaining: estimatedRemaining));
                    }

                    if (settings.Thumbnail && ThumbnailGenerator.IsImage(path: file.FileName))
                    {
                        string thumbnailPath = Path.Combine(path1: settings.Paths.Thumbnail ?? string.Empty, path2: relativePath);
                        thumbnailPath = Path.Combine(
                            path1: Path.GetDirectoryName(path: thumbnailPath) ?? string.Empty,
                            path2: Path.GetFileNameWithoutExtension(path: thumbnailPath) + "(" + Path.GetExtension(path: thumbnailPath).TrimStart(trimChar: '.') + ").jpg");
                        try
                        {
                            await GenerateThumbnailAsync(
                                    source: temp,
                                    output: thumbnailPath,
                                    settings: settings.ThumbnailSettings,
                                    hash: Convert.ToHexStringLower(inArray: originalHash),
                                    cancellationToken: cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            result.Errors++;
                            _logger?.Add(
                                message: $"Could not create thumbnail for {file.FileName}.{Environment.NewLine}{ex}",
                                type: LogType.Error,
                                function: nameof(ExecuteAsync));
                        }
                    }
                    else if (settings.Thumbnail)
                    {
                        _logger?.Add(
                            message: $"Could not generate thumbnail for [{file.FileName}]. Not supported image format.",
                            type: LogType.Info,
                            function: nameof(ExecuteAsync));
                    }

                    if (!SaveToRoots(
                            settings: settings,
                            relativePath: relativePath,
                            temp: temp,
                            hash: originalHash,
                            cancellationToken: cancellationToken))
                    {
                        throw new IOException(message: "Could not save file.");
                    }

                    result.FilesDone++;
                    if (settings.DeleteFiles)
                    {
                        toDelete.Add(item: file);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.Errors++;
                    _logger?.Add(message: ex.ToString(), type: LogType.Error, function: nameof(ExecuteAsync));
                }
                finally
                {
                    if (File.Exists(path: temp))
                    {
                        if (!file.Size.HasValue)
                        {
                            processedBytes = new FileInfo(fileName: temp).Length;
                        }

                        File.Delete(path: temp);
                    }
                }

                bytesCompleted += processedBytes;
                int completed = index + 1;
                estimatedRemaining = useByteEstimate && bytesCompleted > 0
                    ? EstimateRemaining(importTimer.Elapsed, bytesCompleted, plan.TotalBytes)
                    : EstimateRemaining(importTimer.Elapsed, completed, plan.Files.Count);
                progress?.Report(value: new ImportProgress(
                    stage: ImportStage.Downloading,
                    completed: completed,
                    total: plan.Files.Count,
                    currentFile: file.FileName,
                    bytesCompleted: bytesCompleted,
                    bytesTotal: plan.TotalBytes,
                    estimatedRemaining: estimatedRemaining));
            }

            importTimer.Restart();
            estimatedRemaining = TimeSpan.Zero;
            foreach (MediaItem source in toDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(value: new ImportProgress(
                    stage: ImportStage.Deleting,
                    completed: result.Deleted,
                    total: toDelete.Count,
                    currentFile: source.FileName,
                    estimatedRemaining: estimatedRemaining));
                await device.DeleteAsync(source, cancellationToken).ConfigureAwait(false);
                result.Deleted++;
                estimatedRemaining = EstimateRemaining(importTimer.Elapsed, result.Deleted, toDelete.Count);
                progress?.Report(value: new ImportProgress(
                    stage: ImportStage.Deleting,
                    completed: result.Deleted,
                    total: toDelete.Count,
                    currentFile: source.FileName,
                    estimatedRemaining: estimatedRemaining));
            }
        }
        finally
        {
            _logger?.Stop();
        }

        progress?.Report(value: new ImportProgress(
            stage: ImportStage.Completed,
            completed: result.FilesDone,
            total: result.FilesTotal));
        return result;
    }

    private static TimeSpan EstimateRemaining(TimeSpan elapsed, long completed, long total)
    {
        long remaining = total - completed;
        if (completed <= 0 || remaining <= 0 || elapsed <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        double estimatedTicks = elapsed.Ticks * (remaining / (double)completed);
        if (!double.IsFinite(estimatedTicks) || estimatedTicks >= TimeSpan.MaxValue.Ticks)
        {
            return TimeSpan.MaxValue;
        }

        return TimeSpan.FromTicks(value: (long)estimatedTicks);
    }

    private static async Task<byte[]> HashDeviceItemAsync(
        IMediaDevice device,
        MediaItem item,
        CancellationToken cancellationToken)
    {
        using var hash = MD5.Create();
        await using var destination = new CryptoStream(
            Stream.Null,
            hash,
            CryptoStreamMode.Write,
            leaveOpen: true);
        await device.DownloadAsync(item, destination, cancellationToken).ConfigureAwait(false);
        await destination.FlushFinalBlockAsync(cancellationToken);
        return hash.Hash ?? throw new CryptographicException("The device item hash could not be calculated.");
    }

    private static async Task DownloadToFileAsync(
        IMediaDevice device,
        MediaItem item,
        string path,
        CancellationToken cancellationToken)
    {
        await using var destination = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        await device.DownloadAsync(item, destination, cancellationToken).ConfigureAwait(false);
    }

    private static async Task GenerateThumbnailAsync(
        string source,
        string output,
        Thumbnails settings,
        string hash,
        CancellationToken cancellationToken)
    {
        TimeSpan delay = TimeSpan.FromSeconds(value: 2);
        for (var attempt = 0;; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                ThumbnailGenerator.Generate(
                    source: source,
                    output: output,
                    settings: settings,
                    hash: hash);
                return;
            }
            catch (MagickException) when (attempt + 1 < MaxAttempts)
            {
                await Task.Delay(delay: delay, cancellationToken: cancellationToken).ConfigureAwait(false);
                delay += delay;
            }
        }
    }

    private static bool SaveToRoots(
        DownloadSettings settings,
        string relativePath,
        string temp,
        byte[] hash,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> roots = settings.Backup
            ? new[] { settings.Paths.Root, settings.Paths.Backup }
            : new[] { settings.Paths.Root };
        foreach (string root in roots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(value: root))
            {
                return true;
            }

            string destination = Path.Combine(path1: root, path2: relativePath);
            if (File.Exists(path: destination))
            {
                destination = FileOperations.UniqueName(path: destination, sourcePath: temp);
                if (File.Exists(path: destination))
                {
                    continue;
                }
            }

            var copied = false;
            var ownsDestination = false;
            try
            {
                Directory.CreateDirectory(path: Path.GetDirectoryName(path: destination) ?? root);
                if (destination != null)
                {
                    using (new FileStream(path: destination, mode: FileMode.CreateNew))
                    {
                        ownsDestination = true;
                    }

                    for (var attempt = 0; attempt < MaxAttempts && !copied; attempt++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        File.Copy(sourceFileName: temp, destFileName: destination, overwrite: true);
                        copied = !settings.CheckFiles || FileOperations.Verify(expected: hash, path: destination);
                    }
                }
            }
            finally
            {
                if (!copied && ownsDestination && File.Exists(path: destination))
                {
                    File.Delete(path: destination);
                }
            }

            if (!copied)
            {
                return false;
            }
        }

        return true;
    }

    private static DateTime EffectiveDate(MediaItem file)
    {
        return file.CapturedAt?.LocalDateTime ?? default;
    }

    private sealed class DiscoveryProgress(FileLogger logger) : IProgress<MediaDiscoveryUpdate>
    {
        public void Report(MediaDiscoveryUpdate value)
        {
            if (value.Error != null)
            {
                logger.Add(
                    message: $"Could not inspect device directory '{value.CurrentPath}'.{Environment.NewLine}{value.Error}",
                    type: LogType.Error,
                    function: nameof(CreatePlanAsync));
            }
        }
    }
}
