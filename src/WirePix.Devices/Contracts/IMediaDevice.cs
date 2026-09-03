using WirePix.Devices.Enums;
using WirePix.Devices.Models;

namespace WirePix.Devices.Contracts;

/// <summary>
/// Represents a backend-neutral source of media.
/// </summary>
public interface IMediaDevice
{
    /// <summary>
    /// Gets the stable, opaque identifier assigned by the device backend.
    /// </summary>
    /// <remarks>
    /// The identifier must remain stable across discovery refreshes and application
    /// restarts for the same physical device. Consumers must not parse it.
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the name suitable for display in the user interface.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the manufacturer when reported by the device.
    /// </summary>
    string? Manufacturer { get; }

    /// <summary>
    /// Gets the backend-neutral device category.
    /// </summary>
    MediaDeviceType DeviceType { get; }

    /// <summary>
    /// Gets the device's current status snapshot.
    /// </summary>
    MediaDeviceStatus Status { get; }

    /// <summary>
    /// Gets an aggregate storage snapshot, or <see langword="null"/> when unavailable.
    /// </summary>
    MediaDeviceStorage? Storage { get; }

    /// <summary>
    /// Gets the user-visible roots from which this device can provide media.
    /// </summary>
    /// <remarks>
    /// An unusable device can return an empty list when its sources cannot be queried.
    /// </remarks>
    Task<IReadOnlyList<MediaSource>> GetMediaSourcesAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets the media currently exposed by all sources on the device.
    /// </summary>
    /// <remarks>
    /// Each media item must be returned once even when backend source locations overlap.
    /// Recoverable directory failures are reported through <paramref name="progress"/>
    /// and must not prevent sibling directories or sources from being inspected.
    /// This operation should only be attempted when <see cref="Status"/> is usable.
    /// </remarks>
    Task<IReadOnlyList<MediaItem>> GetMediaAsync(
        IProgress<MediaDiscoveryUpdate>? progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Gets media exposed by one selected source.
    /// </summary>
    /// <remarks>
    /// The source must have originated from this device. Its identifier and display
    /// path must not be parsed or converted into local filesystem operations.
    /// Recoverable directory failures are reported through <paramref name="progress"/>
    /// and must not prevent sibling directories from being inspected.
    /// This operation should only be attempted when <see cref="Status"/> is usable.
    /// </remarks>
    Task<IReadOnlyList<MediaItem>> GetMediaAsync(
        MediaSource source,
        IProgress<MediaDiscoveryUpdate>? progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Downloads an item into a caller-owned destination stream.
    /// </summary>
    /// <remarks>
    /// Implementations must not dispose the destination stream. The item must have
    /// originated from this device and its identifier must be treated as opaque.
    /// This operation should only be attempted when <see cref="Status"/> is usable.
    /// </remarks>
    Task DownloadAsync(
        MediaItem item,
        Stream destination,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an item from the device.
    /// </summary>
    /// <remarks>
    /// The item must have originated from this device and its identifier must be
    /// treated as opaque. This operation should only be attempted when
    /// <see cref="Status"/> is usable.
    /// </remarks>
    Task DeleteAsync(
        MediaItem item,
        CancellationToken cancellationToken);
}
