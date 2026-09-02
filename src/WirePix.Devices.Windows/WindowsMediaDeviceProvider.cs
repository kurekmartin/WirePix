using MediaDevices;
using Usb.Events;
using WirePix.Devices.Contracts;
using WirePix.Devices.Enums;
using WirePix.Devices.Models;

namespace WirePix.Devices.Windows;

/// <summary>
/// Discovers Windows portable media devices through the MediaDevices library.
/// </summary>
public sealed class WindowsMediaDeviceProvider : IMediaDeviceProvider, IDisposable
{
    private readonly IUsbEventWatcher _usbEventWatcher;
    private bool _disposed;

    public WindowsMediaDeviceProvider()
    {
        _usbEventWatcher = new UsbEventWatcher();
        _usbEventWatcher.UsbDeviceAdded += OnUsbDevicesChanged;
        _usbEventWatcher.UsbDeviceRemoved += OnUsbDevicesChanged;
    }

    public event EventHandler? DevicesChanged;

    public Task<IReadOnlyList<IMediaDevice>> GetDevicesAsync(
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Task.Run<IReadOnlyList<IMediaDevice>>(
            function: () => GetDevices(cancellationToken),
            cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _usbEventWatcher.UsbDeviceAdded -= OnUsbDevicesChanged;
        _usbEventWatcher.UsbDeviceRemoved -= OnUsbDevicesChanged;
        if (_usbEventWatcher is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static IReadOnlyList<IMediaDevice> GetDevices(
        CancellationToken cancellationToken)
    {
        var devices = new List<IMediaDevice>();
        foreach (MediaDevice mediaDevice in MediaDevice.GetDevices())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                mediaDevice.Connect();
                string protocol = mediaDevice.Protocol ?? string.Empty;
                if (!IsSupportedProtocol(protocol))
                {
                    continue;
                }

                devices.Add(WindowsMediaDevice.CreateReady(mediaDevice, protocol));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                devices.Add(WindowsMediaDevice.CreateUnavailable(
                    mediaDevice,
                    StatusFrom(exception)));
            }
            finally
            {
                DisconnectAndDispose(mediaDevice);
            }
        }

        return devices;
    }

    private static bool IsSupportedProtocol(string protocol)
    {
        return protocol.Contains("MTP", StringComparison.OrdinalIgnoreCase) ||
               protocol.Contains("PTP", StringComparison.OrdinalIgnoreCase);
    }

    internal static MediaDeviceStatus StatusFrom(Exception exception)
    {
        MediaDeviceState state = exception switch
        {
            UnauthorizedAccessException => MediaDeviceState.AccessDenied,
            NotConnectedException => MediaDeviceState.Unavailable,
            _ => MediaDeviceState.Error
        };
        return new MediaDeviceStatus(state, exception.Message);
    }

    private static void DisconnectAndDispose(MediaDevice device)
    {
        try
        {
            if (device.IsConnected)
            {
                device.Disconnect();
            }
        }
        catch
        {
            // Device teardown must not prevent other devices from being discovered.
        }

        try
        {
            device.Dispose();
        }
        catch
        {
            // The device is already unusable and has no managed resources to recover.
        }
    }

    private void OnUsbDevicesChanged(object? sender, UsbDevice device)
    {
        if (!_disposed)
        {
            DevicesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
