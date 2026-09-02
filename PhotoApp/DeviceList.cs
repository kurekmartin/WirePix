using MediaDevices;
using WirePix.Core.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace PhotoApp;

public class DeviceList : ObservableObject
{
    [XmlElement]
    public ObservableCollection<DeviceInfo> DeviceInfo = new();

    [XmlIgnore]
    public IEnumerable<DeviceInfo> ConnectedDevicesInfo { get; set; }

    [XmlIgnore]
    private Device _selectedDevice;

    [XmlIgnore]
    private static readonly string _dataFile = Path.Combine(path1: Application.Current.Resources[key: Properties.Keys.DataFolder].ToString(), path2: "Devices.xml");

    public DeviceList()
    {
        DeviceInfo.CollectionChanged += DeviceInfo_CollectionChanged;
        ConnectedDevicesInfo = new List<DeviceInfo>();
    }

    public void Load()
    {
        if (File.Exists(path: _dataFile))
        {
            var serializer = new XmlSerializer(type: typeof(DeviceList));
            using (FileStream fs = File.OpenRead(path: _dataFile))
            {
                var deviceList = (DeviceList)serializer.Deserialize(stream: fs);
                DeviceInfo = deviceList.DeviceInfo;
                fs.Close();
            }
        }
    }

    private void DeviceInfo_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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

        ConnectedDevicesInfo = DeviceInfo.Where(predicate: d => d.Connected == true);
        OnPropertyChanged(propertyName: "ConnectedDevicesInfo");
    }

    private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        ConnectedDevicesInfo = DeviceInfo.Where(predicate: d => d.Connected == true);
        OnPropertyChanged(propertyName: "ConnectedDevicesInfo");
    }

    public void Save()
    {
        Directory.CreateDirectory(path: Application.Current.Resources[key: Properties.Keys.DataFolder].ToString());
        var serializer = new XmlSerializer(type: typeof(DeviceList));
        using (FileStream fs = File.Create(path: _dataFile))
        {
            TextWriter writer = new StreamWriter(stream: fs);
            serializer.Serialize(textWriter: writer, o: this);
            writer.Close();
            fs.Close();
        }
    }

    public int UpdateDevices()
    {
        IEnumerable<MediaDevice> devices = MediaDevice.GetDevices().Where(predicate: d =>
        {
            d.Connect();
            bool isMediaDevice = d.Protocol.ToUpper().Contains(value: "MTP") || d.Protocol.ToUpper().Contains(value: "PTP"); //filtr podle protokolu
            d.Disconnect();
            return isMediaDevice;
        });
        DeviceInfo.Select(selector: d =>
        {
            d.Connected = false;
            return d;
        }).ToList();

        foreach (MediaDevice device in devices)
        {
            IEnumerable<DeviceInfo> deviceInfo = DeviceInfo.Where(predicate: d => d.Id == device.DeviceId);
            if (deviceInfo.Count() == 0)
            {
                DeviceInfo.Add(item: new DeviceInfo(name: device.Description, id: device.DeviceId, connected: true));
            }
            else
            {
                DeviceInfo deviceOnline = DeviceInfo.First(predicate: d => d.Id == device.DeviceId);
                deviceOnline.Connected = true;
            }
        }

        DeviceInfo.OrderByDescending(keySelector: d => d.Name);
        ConnectedDevicesInfo = DeviceInfo.Where(predicate: d => d.Connected == true);
        OnPropertyChanged(propertyName: "ConnectedDevicesInfo");

        if (ConnectedDevicesInfo.Count() > 0 && SelectedDeviceIndex == -1)
        {
            SelectDevice(index: 0);
        }
        else
        {
            SelectDevice(index: SelectedDeviceIndex);
        }

        return SelectedDeviceIndex;
    }

    public void SelectDevice(int index)
    {
        if (index > -1)
        {
            MediaDevice newDevice = MediaDevice.GetDevices().First(predicate: d => d.DeviceId == ConnectedDevicesInfo.ElementAt(index: index).Id);
            if (SelectedDevice == null || newDevice.DeviceId != SelectedDevice.Id)
            {
                _selectedDevice = new Device(mediaDevice: newDevice);
            }
        }
        else
        {
            _selectedDevice = null;
        }

        OnPropertyChanged(propertyName: "SelectedDeviceInfo");
        OnPropertyChanged(propertyName: "SelectedDevice");
    }

    public Device SelectedDevice => _selectedDevice;

    public DeviceInfo SelectedDeviceInfo
    {
        get
        {
            if (ConnectedDevicesInfo.Count() > 0 && _selectedDevice != null)
            {
                return ConnectedDevicesInfo.First(predicate: d => d.Id == _selectedDevice.Id);
            }
            else
            {
                return new DeviceInfo();
            }
        }
    }

    public int SelectedDeviceIndex
    {
        get
        {
            if (_selectedDevice != null)
            {
                return ConnectedDevicesInfo.ToList().FindIndex(match: d => d.Id == _selectedDevice.Id);
            }

            return -1;
        }
    }
}