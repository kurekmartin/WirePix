using System;
using System.Collections.Generic;
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

    public static async Task<ImportPlan> CreatePlanAsync(
        IMediaDevice device,
        DownloadSettings settings,
        IProgress<ImportProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);
        progress?.Report(value: new ImportProgress(stage: ImportStage.Searching, completed: 0, total: 0));
        IReadOnlyList<MediaItem> files = await device.GetMediaAsync(cancellationToken).ConfigureAwait(false);

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
            Date = settings?.Date
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
                    currentFile: file.FileName));
                string temp = Path.Combine(path1: _tempFolder, path2: Guid.NewGuid().ToString(format: "N"));
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
                            currentFile: file.FileName));
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
                            hash: originalHash))
                    {
                        throw new IOException(message: "Could not save file.");
                    }

                    result.FilesDone++;
                    if (settings.DeleteFiles)
                    {
                        toDelete.Add(item: file);
                    }
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
                        File.Delete(path: temp);
                    }
                }
            }

            foreach (MediaItem source in toDelete)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(value: new ImportProgress(
                    stage: ImportStage.Deleting,
                    completed: result.Deleted,
                    total: toDelete.Count,
                    currentFile: source.FileName));
                await device.DeleteAsync(source, cancellationToken).ConfigureAwait(false);
                result.Deleted++;
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

    private static async Task<byte[]> HashDeviceItemAsync(
        IMediaDevice device,
        MediaItem item,
        CancellationToken cancellationToken)
    {
        using var hash = MD5.Create();
        using var destination = new CryptoStream(
            Stream.Null,
            hash,
            CryptoStreamMode.Write,
            leaveOpen: true);
        await device.DownloadAsync(item, destination, cancellationToken).ConfigureAwait(false);
        destination.FlushFinalBlock();
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
        var delay = TimeSpan.FromSeconds(value: 2);
        for (var attempt = 0; ; attempt++)
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
        byte[] hash)
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

    private static DateTime EffectiveDate(MediaItem file)
    {
        return file.CapturedAt?.LocalDateTime ?? default;
    }
}
