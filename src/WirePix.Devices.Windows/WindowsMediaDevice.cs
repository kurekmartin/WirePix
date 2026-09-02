using MediaDevices;
using WirePix.Devices.Contracts;
using WirePix.Devices.Enums;
using WirePix.Devices.Models;

namespace WirePix.Devices.Windows;

internal sealed class WindowsMediaDevice : IMediaDevice
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".gif", ".heic", ".heif", ".jpeg", ".jpg", ".png", ".tif", ".tiff", ".webp",
        ".arw", ".cr2", ".cr3", ".dng", ".nef", ".orf", ".raf", ".rw2"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".3gp", ".avi", ".m4v", ".mkv", ".mov", ".mp4", ".mpeg", ".mpg", ".mts", ".webm"
    };

    private readonly SemaphoreSlim _operations = new(1, 1);
    private readonly IReadOnlyList<MediaSource> _sources;

    private WindowsMediaDevice(
        string id,
        string displayName,
        string? manufacturer,
        MediaDeviceType deviceType,
        MediaDeviceStatus status,
        MediaDeviceStorage? storage,
        IReadOnlyList<MediaSource> sources)
    {
        Id = id;
        DisplayName = displayName;
        Manufacturer = manufacturer;
        DeviceType = deviceType;
        Status = status;
        Storage = storage;
        _sources = sources;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string? Manufacturer { get; }
    public MediaDeviceType DeviceType { get; }
    public MediaDeviceStatus Status { get; }
    public MediaDeviceStorage? Storage { get; }

    internal static WindowsMediaDevice CreateReady(
        MediaDevice device,
        string protocol)
    {
        IReadOnlyList<MediaSource> sources = CreateSources(device, protocol);
        long total = sources.Sum(source => source.Storage?.TotalBytes ?? 0);
        long available = sources.Sum(source => source.Storage?.AvailableBytes ?? 0);
        MediaDeviceStorage? storage = sources.Count == 0
            ? null
            : new MediaDeviceStorage(total, available);

        return new WindowsMediaDevice(
            device.DeviceId,
            device.Description ?? string.Empty,
            device.Manufacturer,
            DeviceTypeFrom(protocol),
            new MediaDeviceStatus(MediaDeviceState.Ready),
            storage,
            sources);
    }

    internal static WindowsMediaDevice CreateUnavailable(
        MediaDevice device,
        MediaDeviceStatus status)
    {
        return new WindowsMediaDevice(
            device.DeviceId,
            device.Description ?? string.Empty,
            device.Manufacturer,
            MediaDeviceType.Unknown,
            status,
            null,
            Array.Empty<MediaSource>());
    }

    public Task<IReadOnlyList<MediaSource>> GetMediaSourcesAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_sources);
    }

    public Task<IReadOnlyList<MediaItem>> GetMediaAsync(
        CancellationToken cancellationToken)
    {
        return RunConnectedAsync(
            operation: device => EnumerateMedia(device, sourceId: null, cancellationToken),
            cancellationToken);
    }

    public Task<IReadOnlyList<MediaItem>> GetMediaAsync(
        MediaSource source,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (_sources.All(candidate => candidate.Id != source.Id))
        {
            throw new ArgumentException("The media source does not belong to this device.", nameof(source));
        }

        return RunConnectedAsync(
            operation: device => EnumerateMedia(device, source.Id, cancellationToken),
            cancellationToken);
    }

    public Task DownloadAsync(
        MediaItem item,
        Stream destination,
        CancellationToken cancellationToken)
    {
        ValidateItem(item);
        ArgumentNullException.ThrowIfNull(destination);
        return RunConnectedAsync(
            operation: device => device.DownloadFileFromPersistentUniqueId(item.Id, destination),
            cancellationToken);
    }

    public Task DeleteAsync(
        MediaItem item,
        CancellationToken cancellationToken)
    {
        ValidateItem(item);
        return RunConnectedAsync(
            operation: device =>
            {
                MediaFileSystemInfo info = device.GetFileSystemInfoFromPersistentUniqueId(item.Id);
                device.DeleteFile(info.FullName);
            },
            cancellationToken);
    }

    private static IReadOnlyList<MediaSource> CreateSources(
        MediaDevice device,
        string protocol)
    {
        var sources = new List<MediaSource>();
        foreach (MediaDriveInfo drive in device.GetDrives())
        {
            if (drive.RootDirectory == null)
            {
                continue;
            }

            string id = SourceId(drive);
            string displayName = !string.IsNullOrWhiteSpace(drive.VolumeLabel)
                ? drive.VolumeLabel
                : !string.IsNullOrWhiteSpace(drive.Name)
                    ? drive.Name
                    : drive.RootDirectory.Name;
            var storage = new MediaDeviceStorage(
                Convert.ToInt64(drive.TotalSize),
                Convert.ToInt64(drive.AvailableFreeSpace));
            sources.Add(new MediaSource(
                id,
                displayName,
                drive.RootDirectory.FullName,
                SourceTypeFrom(protocol, drive),
                storage));
        }

        return sources;
    }

    private static IReadOnlyList<MediaItem> EnumerateMedia(
        MediaDevice device,
        string? sourceId,
        CancellationToken cancellationToken)
    {
        var result = new List<MediaItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (MediaDriveInfo drive in device.GetDrives())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (drive.RootDirectory == null ||
                sourceId != null && SourceId(drive) != sourceId)
            {
                continue;
            }

            var dcimDirectories = new List<string>();
            FindDcimDirectories(
                device,
                drive.RootDirectory.FullName,
                dcimDirectories,
                cancellationToken);
            foreach (string dcim in dcimDirectories)
            {
                EnumerateDirectory(
                    device,
                    dcim,
                    SourceId(drive),
                    result,
                    seen,
                    cancellationToken);
            }
        }

        return result;
    }

    private static void FindDcimDirectories(
        MediaDevice device,
        string path,
        List<string> result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<string> directories = [.. device.EnumerateDirectories(path)];
        string? dcim = directories.FirstOrDefault(directory =>
            string.Equals(
                Path.GetFileName(directory.TrimEnd('\\', '/')),
                "DCIM",
                StringComparison.OrdinalIgnoreCase));
        if (dcim != null)
        {
            result.Add(dcim);
            return;
        }

        foreach (string child in directories)
        {
            string name = Path.GetFileName(child.TrimEnd('\\', '/'));
            if (!string.IsNullOrEmpty(name) && !name.StartsWith('.'))
            {
                FindDcimDirectories(device, child, result, cancellationToken);
            }
        }
    }

    private static void EnumerateDirectory(
        MediaDevice device,
        string path,
        string sourceId,
        List<MediaItem> result,
        HashSet<string> seen,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MediaDirectoryInfo directory = device.GetDirectoryInfo(path);
        foreach (MediaFileInfo file in directory.EnumerateFiles("*"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrEmpty(file.PersistentUniqueId) && seen.Add(file.PersistentUniqueId))
            {
                DateTime? captured = CapturedAt(file);
                result.Add(new MediaItem(
                    file.PersistentUniqueId,
                    sourceId,
                    file.Name,
                    Convert.ToInt64(file.Length),
                    captured.HasValue ? new DateTimeOffset(captured.Value) : null,
                    KindFrom(file.Name)));
            }
        }

        foreach (string child in device.EnumerateDirectories(path))
        {
            string name = Path.GetFileName(child.TrimEnd('\\', '/'));
            if (!string.IsNullOrEmpty(name) && !name.StartsWith('.'))
            {
                EnumerateDirectory(device, child, sourceId, result, seen, cancellationToken);
            }
        }
    }

    private async Task<T> RunConnectedAsync<T>(
        Func<MediaDevice, T> operation,
        CancellationToken cancellationToken)
    {
        await _operations.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(
                function: () => WithConnectedDevice(operation, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _operations.Release();
        }
    }

    private async Task RunConnectedAsync(
        Action<MediaDevice> operation,
        CancellationToken cancellationToken)
    {
        await RunConnectedAsync(
            operation: device =>
            {
                operation(device);
                return true;
            },
            cancellationToken).ConfigureAwait(false);
    }

    private T WithConnectedDevice<T>(
        Func<MediaDevice, T> operation,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<MediaDevice> candidates = [.. MediaDevice.GetDevices()];
        MediaDevice? device = candidates.FirstOrDefault(candidate => candidate.DeviceId == Id);
        foreach (MediaDevice candidate in candidates)
        {
            if (!ReferenceEquals(candidate, device))
            {
                candidate.Dispose();
            }
        }

        if (device == null)
        {
            throw new IOException($"Media device '{Id}' is no longer available.");
        }

        using (device)
        {
            try
            {
                device.Connect();
                cancellationToken.ThrowIfCancellationRequested();
                return operation(device);
            }
            finally
            {
                if (device.IsConnected)
                {
                    device.Disconnect();
                }
            }
        }
    }

    private void ValidateItem(MediaItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_sources.All(source => source.Id != item.SourceId))
        {
            throw new ArgumentException("The media item does not belong to this device.", nameof(item));
        }
    }

    private static string SourceId(MediaDriveInfo drive)
    {
        return string.IsNullOrEmpty(drive.RootDirectory.PersistentUniqueId)
            ? drive.RootDirectory.FullName
            : drive.RootDirectory.PersistentUniqueId;
    }

    private static MediaDeviceType DeviceTypeFrom(string protocol)
    {
        return protocol.Contains("PTP", StringComparison.OrdinalIgnoreCase)
            ? MediaDeviceType.Camera
            : protocol.Contains("MTP", StringComparison.OrdinalIgnoreCase)
                ? MediaDeviceType.Phone
                : MediaDeviceType.Unknown;
    }

    private static MediaSourceType SourceTypeFrom(
        string protocol,
        MediaDriveInfo drive)
    {
        if (protocol.Contains("PTP", StringComparison.OrdinalIgnoreCase))
        {
            return MediaSourceType.CameraStorage;
        }

        return drive.DriveType == DriveType.Removable
            ? MediaSourceType.RemovableStorage
            : MediaSourceType.InternalStorage;
    }

    private static MediaKind KindFrom(string fileName)
    {
        string extension = Path.GetExtension(fileName);
        if (ImageExtensions.Contains(extension))
        {
            return MediaKind.Image;
        }

        return VideoExtensions.Contains(extension)
            ? MediaKind.Video
            : MediaKind.Unknown;
    }

    private static DateTime? CapturedAt(MediaFileInfo file)
    {
        if (file.CreationTime is { } creationTime && creationTime != default)
        {
            return creationTime;
        }

        if (file.DateAuthored is { } dateAuthored && dateAuthored != default)
        {
            return dateAuthored;
        }

        return file.LastWriteTime is { } lastWriteTime && lastWriteTime != default
            ? lastWriteTime
            : null;
    }
}
