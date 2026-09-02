using WirePix.Devices.Enums;

namespace WirePix.Devices.Models;

/// <summary>
/// Describes a media object without exposing backend paths or protocol objects.
/// </summary>
/// <param name="Id">An opaque identifier assigned by the originating device backend.</param>
/// <param name="SourceId">The opaque identifier of the source containing the item.</param>
/// <param name="FileName">The file name presented to the application.</param>
/// <param name="Size">The size in bytes, or <see langword="null"/> when unknown.</param>
/// <param name="CapturedAt">The capture time, or <see langword="null"/> when unknown.</param>
/// <param name="Kind">The media category.</param>
public sealed record MediaItem(
    string Id,
    string SourceId,
    string FileName,
    long? Size,
    DateTimeOffset? CapturedAt,
    MediaKind Kind);
