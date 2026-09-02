using WirePix.Devices.Enums;

namespace WirePix.Devices.Models;

/// <summary>
/// Describes a user-visible and selectable location exposed by a media device.
/// </summary>
/// <param name="Id">
/// A stable, opaque source identifier used for selection and persisted filtering.
/// It remains stable across discovery refreshes and application restarts while the
/// same logical source exists.
/// </param>
/// <param name="DisplayName">The user-facing source name.</param>
/// <param name="DevicePath">
/// The backend-native path shown to the user. It is presentation data and may not be
/// accessible through the local filesystem.
/// </param>
/// <param name="SourceType">The backend-neutral source category.</param>
/// <param name="Storage">Source-specific storage information, when available.</param>
public sealed record MediaSource(
    string Id,
    string DisplayName,
    string DevicePath,
    MediaSourceType SourceType,
    MediaDeviceStorage? Storage);
