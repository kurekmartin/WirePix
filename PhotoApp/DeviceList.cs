using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using PhotoApp.Properties;
using WirePix.Devices.Contracts;

namespace PhotoApp;

public class DeviceList : ObservableObject
{
    private readonly IMediaDeviceProvider _provider;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyDictionary<string, IMediaDevice> _devices =
        new Dictionary<string, IMediaDevice>();

    [XmlElement]
    public ObservableCollection<DeviceInfo> DeviceInfo = new();

    [XmlIgnore]
    public IEnumerable<DeviceInfo> ConnectedDevicesInfo { get; set; }

    [XmlIgnore]
    private Device _selectedDevice;

    [XmlIgnore]
    private static readonly string _dataFile = Path.Combine(
        path1: Application.Current.Resources[key: Keys.DataFolder].ToString(),
        path2: "Devices.xml");

    public DeviceList()
        : this(EmptyMediaDeviceProvider.Instance)
    {
    }

    public DeviceList(IMediaDeviceProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        SubscribeToDeviceInfo();
        ConnectedDevicesInfo = new List<DeviceInfo>();
    }

    public void Load()
    {
        if (!File.Exists(path: _dataFile))
        {
            return;
        }

        var serializer = new XmlSerializer(type: typeof(DeviceList));
        using FileStream fs = File.OpenRead(path: _dataFile);
        var deviceList = (DeviceList)serializer.Deserialize(stream: fs);
        DeviceInfo.CollectionChanged -= DeviceInfo_CollectionChanged;
        DeviceInfo = deviceList.DeviceInfo;
        SubscribeToDeviceInfo();
    }

    public void Save()
    {
        Directory.CreateDirectory(path: Application.Current.Resources[key: Keys.DataFolder].ToString());
        var serializer = new XmlSerializer(type: typeof(DeviceList));
        using FileStream fs = File.Create(path: _dataFile);
        using TextWriter writer = new StreamWriter(stream: fs);
        serializer.Serialize(textWriter: writer, o: this);
    }

    public async Task<int> UpdateDevicesAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            string selectedId = _selectedDevice?.Id;
            IReadOnlyList<IMediaDevice> devices = await _provider.GetDevicesAsync(cancellationToken);
            _devices = devices
                .GroupBy(device => device.Id)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (DeviceInfo info in DeviceInfo)
            {
                info.Connected = false;
            }

            foreach (IMediaDevice device in _devices.Values)
            {
                DeviceInfo info = DeviceInfo.FirstOrDefault(candidate => candidate.Id == device.Id);
                if (info == null)
                {
                    DeviceInfo.Add(item: new DeviceInfo(
                        name: device.DisplayName,
                        id: device.Id,
                        connected: true));
                }
                else
                {
                    info.Connected = true;
                }
            }

            RefreshConnectedDevices();
            if (selectedId != null && _devices.TryGetValue(selectedId, out IMediaDevice selected))
            {
                SetSelectedDevice(selected);
            }
            else if (ConnectedDevicesInfo.Any())
            {
                SelectDevice(index: 0);
            }
            else
            {
                SetSelectedDevice(mediaDevice: null);
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
        List<DeviceInfo> connected = ConnectedDevicesInfo.ToList();
        if (index < 0 || index >= connected.Count)
        {
            SetSelectedDevice(mediaDevice: null);
            return;
        }

        SetSelectedDevice(_devices.TryGetValue(connected[index].Id, out IMediaDevice device)
            ? device
            : null);
    }

    public Device SelectedDevice => _selectedDevice;

    public DeviceInfo SelectedDeviceInfo
    {
        get
        {
            if (_selectedDevice != null)
            {
                return ConnectedDevicesInfo.FirstOrDefault(info => info.Id == _selectedDevice.Id)
                       ?? new DeviceInfo();
            }

            return new DeviceInfo();
        }
    }

    public int SelectedDeviceIndex => _selectedDevice == null
        ? -1
        : ConnectedDevicesInfo.ToList().FindIndex(match: info => info.Id == _selectedDevice.Id);

    private void SubscribeToDeviceInfo()
    {
        DeviceInfo.CollectionChanged += DeviceInfo_CollectionChanged;
        foreach (INotifyPropertyChanged item in DeviceInfo)
        {
            item.PropertyChanged += Item_PropertyChanged;
        }
    }

    private void DeviceInfo_CollectionChanged(
        object sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (INotifyPropertyChanged item in e.OldItems)
            {
                item.PropertyChanged -= Item_PropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (INotifyPropertyChanged item in e.NewItems)
            {
                item.PropertyChanged += Item_PropertyChanged;
            }
        }

        RefreshConnectedDevices();
    }

    private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        RefreshConnectedDevices();
    }

    private void RefreshConnectedDevices()
    {
        ConnectedDevicesInfo = DeviceInfo.Where(predicate: device => device.Connected == true).ToList();
        OnPropertyChanged(propertyName: nameof(ConnectedDevicesInfo));
    }

    private void SetSelectedDevice(IMediaDevice mediaDevice)
    {
        if (mediaDevice == null)
        {
            _selectedDevice = null;
        }
        else if (_selectedDevice == null || _selectedDevice.Id != mediaDevice.Id)
        {
            _selectedDevice = new Device(mediaDevice);
        }
        else
        {
            _selectedDevice.UpdateSource(mediaDevice);
        }

        OnPropertyChanged(propertyName: nameof(SelectedDeviceInfo));
        OnPropertyChanged(propertyName: nameof(SelectedDevice));
    }

    private sealed class EmptyMediaDeviceProvider : IMediaDeviceProvider
    {
        internal static readonly EmptyMediaDeviceProvider Instance = new();

        public event EventHandler DevicesChanged
        {
            add { }
            remove { }
        }

        public Task<IReadOnlyList<IMediaDevice>> GetDevicesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<IMediaDevice>>(Array.Empty<IMediaDevice>());
        }
    }
}
