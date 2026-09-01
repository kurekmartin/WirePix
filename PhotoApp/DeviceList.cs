using MediaDevices;
using WirePix.Core.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml.Serialization;

namespace PhotoApp
{
    public class DeviceList : ObservableObject
    {
        [XmlElement]
        public ObservableCollection<DeviceInfo> DeviceInfo = new ObservableCollection<DeviceInfo>();
        [XmlIgnore]
        public IEnumerable<DeviceInfo> ConnectedDevicesInfo { get; set; }
        [XmlIgnore]
        private Device _selectedDevice;
        [XmlIgnore]
        private static readonly string _dataFile = Path.Combine(Application.Current.Resources[Properties.Keys.DataFolder].ToString(), "Devices.xml");

        public DeviceList()
        {
            DeviceInfo.CollectionChanged += DeviceInfo_CollectionChanged;
            ConnectedDevicesInfo = new List<DeviceInfo>();
        }

        public void Load()
        {
            if (File.Exists(_dataFile))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(DeviceList));
                using (FileStream fs = File.OpenRead(_dataFile))
                {
                    DeviceList deviceList = (DeviceList)serializer.Deserialize(fs);
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
                    item.PropertyChanged -= Item_PropertyChanged;
            }
            if (e.NewItems != null)
            {
                foreach (INotifyPropertyChanged item in e.NewItems)
                    item.PropertyChanged += Item_PropertyChanged;
            }
            ConnectedDevicesInfo = DeviceInfo.Where(d => d.Connected == true);
            OnPropertyChanged("ConnectedDevicesInfo");
        }

        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ConnectedDevicesInfo = DeviceInfo.Where(d => d.Connected == true);
            OnPropertyChanged("ConnectedDevicesInfo");
        }

        public void Save()
        {
            Directory.CreateDirectory(Application.Current.Resources[Properties.Keys.DataFolder].ToString());
            XmlSerializer serializer = new XmlSerializer(typeof(DeviceList));
            using (FileStream fs = File.Create(_dataFile))
            {
                TextWriter writer = new StreamWriter(fs);
                serializer.Serialize(writer, this);
                writer.Close();
                fs.Close();
            }
        }

        public int UpdateDevices()
        {
            IEnumerable<MediaDevice> devices = MediaDevice.GetDevices().Where(d =>
            {
                d.Connect();
                bool isMediaDevice = d.Protocol.ToUpper().Contains("MTP") || d.Protocol.ToUpper().Contains("PTP"); //filtr podle protokolu
                d.Disconnect();
                return isMediaDevice;
            });
            DeviceInfo.Select(d => { d.Connected = false; return d; }).ToList();

            foreach (MediaDevice device in devices)
            {
                var deviceInfo = DeviceInfo.Where(d => d.ID == device.DeviceId);
                if (deviceInfo.Count() == 0)
                {
                    DeviceInfo.Add(new DeviceInfo(device.Description, device.DeviceId, connected: true));
                }
                else
                {
                    var deviceOnline = DeviceInfo.First(d => d.ID == device.DeviceId);
                    deviceOnline.Connected = true;
                }
            }
            DeviceInfo.OrderByDescending(d => d.Name);
            ConnectedDevicesInfo = DeviceInfo.Where(d => d.Connected == true);
            OnPropertyChanged("ConnectedDevicesInfo");

            if(ConnectedDevicesInfo.Count() > 0 && SelectedDeviceIndex == -1)
            {
                SelectDevice(0);
            }
            else
            {
                SelectDevice(SelectedDeviceIndex);
            }

            return SelectedDeviceIndex;
        }

        public void SelectDevice(int index)
        {
            if (index > -1)
            {
                var newDevice = MediaDevice.GetDevices().First(d => d.DeviceId == ConnectedDevicesInfo.ElementAt(index).ID);
                if(SelectedDevice == null || newDevice.DeviceId != SelectedDevice.ID)
                {
                    _selectedDevice = new Device(newDevice);
                }
            }
            else
            {
                _selectedDevice = null;
            }
            OnPropertyChanged("SelectedDeviceInfo");
            OnPropertyChanged("SelectedDevice");
        }

        public Device SelectedDevice
        {
            get
            {
                return _selectedDevice;
            }
        }

        public DeviceInfo SelectedDeviceInfo
        {
            get
            {
                if (ConnectedDevicesInfo.Count() > 0 && _selectedDevice != null)
                {
                    return ConnectedDevicesInfo.First(d => d.ID == _selectedDevice.ID);
                }
                else
                    return new DeviceInfo();

            }
        }

        public int SelectedDeviceIndex
        {
            get
            {
                if (_selectedDevice != null)
                {
                    return ConnectedDevicesInfo.ToList().FindIndex(d => d.ID == _selectedDevice.ID);
                }
                return -1;
            }
        }
    }
}
