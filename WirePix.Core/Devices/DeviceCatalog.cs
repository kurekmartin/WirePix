using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WirePix.Core.Import.Contracts;
using WirePix.Core.Models;
using WirePix.Core.Models.Devices;
using WirePix.Core.Models.Settings;
using WirePix.Devices.Contracts;

namespace WirePix.Core.Devices;

public sealed class DeviceCatalog : ObservableObject
{
    private readonly IMediaDeviceProvider _provider;
    private readonly DeviceHistoryStore _store;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyDictionary<string, IMediaDevice> _devices = new Dictionary<string, IMediaDevice>();

    public DeviceCatalog(IMediaDeviceProvider provider, DeviceHistoryStore store)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        DeviceInfo.CollectionChanged += DeviceInfoChanged;
    }

    public ObservableCollection<DeviceInfo> DeviceInfo { get; } = [];

    public IReadOnlyList<DeviceInfo> ConnectedDevicesInfo
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    } = [];

    public IMediaDevice SelectedDevice { get; private set; }

    public DeviceInfo SelectedDeviceInfo => SelectedDevice == null
        ? null
        : ConnectedDevicesInfo.FirstOrDefault(info => info.Id == SelectedDevice.Id);
    public int SelectedDeviceIndex => SelectedDevice == null
        ? -1
        : ConnectedDevicesInfo.ToList().FindIndex(info => info.Id == SelectedDevice.Id);

    public void Load()
    {
        DeviceInfo.Clear();
        foreach (DeviceInfo info in _store.Load())
        {
            DeviceInfo.Add(info);
        }
        RefreshConnectedDevices();
    }

    public void Save() => _store.Save(DeviceInfo);

    public async Task<int> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            string selectedId = SelectedDevice?.Id;
            IReadOnlyList<IMediaDevice> discovered = await _provider.GetDevicesAsync(cancellationToken);
            _devices = discovered.GroupBy(device => device.Id).ToDictionary(group => group.Key, group => group.First());

            foreach (DeviceInfo info in DeviceInfo)
            {
                info.Connected = false;
            }

            foreach (IMediaDevice device in _devices.Values)
            {
                DeviceInfo info = DeviceInfo.FirstOrDefault(candidate => candidate.Id == device.Id);
                if (info == null)
                {
                    List<DeviceInfo> legacyMatches = DeviceInfo
                        .Where(candidate => string.IsNullOrEmpty(candidate.Id) && candidate.Name == device.DisplayName)
                        .ToList();
                    int liveNameCount = _devices.Values.Count(candidate => candidate.DisplayName == device.DisplayName);
                    if (legacyMatches.Count == 1 && liveNameCount == 1)
                    {
                        info = legacyMatches[0];
                        info.Id = device.Id;
                    }
                    else
                    {
                        info = new DeviceInfo(device.DisplayName, device.Id);
                        DeviceInfo.Add(info);
                    }
                }
                info.Connected = true;
            }

            RefreshConnectedDevices();
            if (selectedId != null && _devices.TryGetValue(selectedId, out IMediaDevice selected))
            {
                SetSelectedDevice(selected);
            }
            else if (ConnectedDevicesInfo.Count > 0)
            {
                SelectDevice(0);
            }
            else
            {
                SetSelectedDevice(null);
            }
            return SelectedDeviceIndex;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public void SelectDevice(int index)
    {
        if (index < 0 || index >= ConnectedDevicesInfo.Count ||
            !_devices.TryGetValue(ConnectedDevicesInfo[index].Id, out IMediaDevice device))
        {
            SetSelectedDevice(null);
            return;
        }
        SetSelectedDevice(device);
    }

    public bool RecordSuccessfulImport(DateTime startedAt, DownloadSettings settings, ImportResult result)
    {
        if (SelectedDeviceInfo == null || settings.DownloadSelect != DownloadSelect.lastBackup ||
            result == null || result.FilesDone != result.FilesTotal)
        {
            return false;
        }
        SelectedDeviceInfo.LastBackup = startedAt;
        Save();
        return true;
    }

    private void SetSelectedDevice(IMediaDevice device)
    {
        SelectedDevice = device;
        OnPropertyChanged(nameof(SelectedDevice));
        OnPropertyChanged(nameof(SelectedDeviceInfo));
        OnPropertyChanged(nameof(SelectedDeviceIndex));
    }

    private void DeviceInfoChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (INotifyPropertyChanged item in e.OldItems) item.PropertyChanged -= DeviceInfoPropertyChanged;
        }
        if (e.NewItems != null)
        {
            foreach (INotifyPropertyChanged item in e.NewItems) item.PropertyChanged += DeviceInfoPropertyChanged;
        }
        RefreshConnectedDevices();
    }

    private void DeviceInfoPropertyChanged(object sender, PropertyChangedEventArgs e) => RefreshConnectedDevices();

    private void RefreshConnectedDevices()
    {
        ConnectedDevicesInfo = DeviceInfo.Where(device => device.Connected).ToList();
        OnPropertyChanged(nameof(SelectedDeviceInfo));
        OnPropertyChanged(nameof(SelectedDeviceIndex));
    }
}
