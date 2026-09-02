using WirePix.Devices.Enums;

namespace WirePix.Devices.Models;

/// <summary>
/// Describes the current usability of a connected media device.
/// </summary>
/// <param name="State">The backend-neutral device state.</param>
/// <param name="Message">Optional backend detail about an error or unusable state.</param>
public sealed record MediaDeviceStatus(
    MediaDeviceState State,
    string? Message = null)
{
    /// <summary>
    /// Gets whether media operations can currently be attempted.
    /// </summary>
    public bool IsUsable => State == MediaDeviceState.Ready;
}
