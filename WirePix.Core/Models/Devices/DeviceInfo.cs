using System;
using System.Xml.Serialization;

namespace WirePix.Core.Models.Devices;

public sealed class DeviceInfo : ObservableObject
{
    private string _name = string.Empty;
    private string _originalName = string.Empty;
    private bool _connected;

    public DeviceInfo()
    {
    }

    public DeviceInfo(string name, string id, DateTime lastBackup = default, bool connected = false)
    {
        _originalName = _name = name;
        Id = id;
        LastBackup = lastBackup;
        _connected = connected;
    }

    public string Id { get; set; }

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
            if (_originalName == string.Empty)
            {
                _originalName = _name;
            }
        }
    }

    public DateTime LastBackup
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            field = value;
            OnPropertyChanged();
        }
    }

    [XmlIgnore] public bool Connected
    {
        get => _connected;
        set
        {
            if (value == _connected)
            {
                return;
            }

            _connected = value;
            OnPropertyChanged();
            if (!_connected && _name == string.Empty)
            {
                _name = _originalName;
            }
        }
    }
}