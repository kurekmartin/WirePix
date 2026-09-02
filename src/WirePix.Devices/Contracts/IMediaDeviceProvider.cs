namespace WirePix.Devices.Contracts;

/// <summary>
/// Discovers media devices that are currently available to the application.
/// </summary>
public interface IMediaDeviceProvider
{
    /// <summary>
    /// Occurs when device connectivity, status, or exposed media sources may have changed.
    /// </summary>
    /// <remarks>
    /// The event can be raised on any thread and multiple underlying changes can be
    /// coalesced into one notification. Consumers should call <see cref="GetDevicesAsync"/>
    /// to obtain a complete new snapshot instead of relying on event-specific data.
    /// </remarks>
    event EventHandler? DevicesChanged;

    /// <summary>
    /// Gets a snapshot of all connected, supported media devices.
    /// </summary>
    /// <remarks>
    /// Devices that are locked, busy, inaccessible, or otherwise unusable are included
    /// with an appropriate <see cref="IMediaDevice.Status"/>. A failure while probing one
    /// device must not prevent other connected devices from being returned.
    /// </remarks>
    Task<IReadOnlyList<IMediaDevice>> GetDevicesAsync(
        CancellationToken cancellationToken);
}
