namespace WirePix.Devices.Enums;

/// <summary>
/// Identifies whether a connected media device can currently be used.
/// </summary>
public enum MediaDeviceState
{
    Unknown,
    Ready,
    Locked,
    Busy,
    AccessDenied,
    Unavailable,
    Error
}
