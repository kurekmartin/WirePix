using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WirePix.Core.Units;
using WirePix.Devices.Contracts;
using WirePix.Devices.Enums;
using WirePix.Devices.Models;

namespace PhotoApp;

public enum FileSearchState
{
    Unknown,
    Searching,
    Ready
}

public sealed class DeviceViewModel(IMediaDevice device) : ObservableObject
{
    internal IMediaDevice Device { get; } = device ?? throw new ArgumentNullException(nameof(device));
    public string Id => Device.Id;
    public string Manufacturer => Device.Manufacturer ?? string.Empty;
    public string Name => Device.DisplayName;
    public MediaDeviceState Status => Device.Status.State;

    public IReadOnlyList<string> MediaDirectories
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = [];

    public FileSearchState FileSearchStatus
    {
        get;
        internal set
        {
            field = value;
            OnPropertyChanged();
        }
    } = FileSearchState.Unknown;

    public DeviceStorageSpace Space
    {
        get
        {
            MediaDeviceStorage storage = Device.Storage;
            if (storage == null)
            {
                return default;
            }

            return new DeviceStorageSpace(
                TotalGigabytes: ByteSizeConverter.ToGigabytes(storage.TotalBytes),
                AvailableGigabytes: ByteSizeConverter.ToGigabytes(storage.AvailableBytes),
                UsedGigabytes: ByteSizeConverter.ToGigabytes(storage.UsedBytes));
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        MediaDirectories = await Device.GetMediaDirectoriesAsync(cancellationToken);
    }
}
