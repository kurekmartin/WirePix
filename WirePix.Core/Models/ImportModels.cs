using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using WirePix.Core.Storage;

namespace WirePix.Core.Models
{
    public enum DownloadSelect
    {
        lastBackup,
        dateRange
    }

    public enum ThumbnailSelect
    {
        longerSide,
        shorterSide
    }

    public sealed class PathStruct : ObservableObject
    {
        private string _root = string.Empty, _backup = string.Empty, _thumbnail = string.Empty;
        private List<List<string>> _folderTags = new List<List<string>>();
        private List<string> _fileTags = new List<string>();

        public string Root
        {
            get => _root;
            set
            {
                if (value != _root)
                {
                    _root = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<List<string>> FolderTags
        {
            get => _folderTags;
            set
            {
                if (value != _folderTags)
                {
                    _folderTags = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> FileTags
        {
            get => _fileTags;
            set
            {
                if (value != _fileTags)
                {
                    _fileTags = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Backup
        {
            get => _backup;
            set
            {
                if (value != _backup)
                {
                    _backup = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Thumbnail
        {
            get => _thumbnail;
            set
            {
                if (value != _thumbnail)
                {
                    _thumbnail = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    public sealed class DateRange : ObservableObject
    {
        private DateTime _start, _end;

        public DateRange()
        {
            Start = End = DateTime.Now.Date;
        }

        public DateTime Start
        {
            get => _start;
            set
            {
                if (value != _start)
                {
                    _start = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime End
        {
            get => _end;
            set
            {
                if (value != _end)
                {
                    _end = value;
                    OnPropertyChanged();
                }
            }
        }

        public static bool operator ==(DateRange a, DateRange b) => ReferenceEquals(a, b) || (a != null && b != null && a.Start == b.Start && a.End == b.End);
        public static bool operator !=(DateRange a, DateRange b) => !(a == b);
        public override bool Equals(object obj) => obj is DateRange other && this == other;
        public override int GetHashCode() => HashCode.Combine(Start, End);
    }

    public sealed class Thumbnails : ObservableObject
    {
        private ThumbnailSelect _selected;
        private int _value;

        public ThumbnailSelect Selected
        {
            get => _selected;
            set
            {
                if (value != _selected)
                {
                    _selected = value;
                    OnPropertyChanged();
                }
            }
        }

        public int Value
        {
            get => _value;
            set
            {
                if (value != _value)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    public sealed class SaveOptions : ObservableObject
    {
        private string _fileName = string.Empty;
        private bool _root, _folderStruct, _fileStruct, _backup, _thumbnails, _fileCheck, _deleteFiles;

        [XmlIgnore] public string FileName
        {
            get => _fileName;
            set
            {
                if (value != _fileName)
                {
                    _fileName = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Root
        {
            get => _root;
            set
            {
                if (value != _root)
                {
                    _root = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool FolderStruct
        {
            get => _folderStruct;
            set
            {
                if (value != _folderStruct)
                {
                    _folderStruct = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool FileStruct
        {
            get => _fileStruct;
            set
            {
                if (value != _fileStruct)
                {
                    _fileStruct = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Backup
        {
            get => _backup;
            set
            {
                if (value != _backup)
                {
                    _backup = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Thumbnails
        {
            get => _thumbnails;
            set
            {
                if (value != _thumbnails)
                {
                    _thumbnails = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool FileCheck
        {
            get => _fileCheck;
            set
            {
                if (value != _fileCheck)
                {
                    _fileCheck = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DeleteFiles
        {
            get => _deleteFiles;
            set
            {
                if (value != _deleteFiles)
                {
                    _deleteFiles = value;
                    OnPropertyChanged();
                }
            }
        }

        public SaveOptions Copy() => (SaveOptions)MemberwiseClone();
    }

    public sealed class DownloadSettings : ObservableObject
    {
        private bool _checkFiles, _deleteFiles, _backup, _thumbnail;
        private DownloadSelect _downloadSelect = DownloadSelect.lastBackup;
        public PathStruct Paths { get; set; } = new PathStruct();
        [XmlIgnore] public DateRange Date { get; set; } = new DateRange();
        public Thumbnails ThumbnailSettings { get; set; } = new Thumbnails();
        public SaveOptions SaveOptions = new SaveOptions();

        public bool CheckFiles
        {
            get => _checkFiles;
            set
            {
                if (value != _checkFiles)
                {
                    _checkFiles = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool DeleteFiles
        {
            get => _deleteFiles;
            set
            {
                if (value != _deleteFiles)
                {
                    _deleteFiles = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Backup
        {
            get => _backup;
            set
            {
                if (value != _backup)
                {
                    _backup = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Thumbnail
        {
            get => _thumbnail;
            set
            {
                if (value != _thumbnail)
                {
                    _thumbnail = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore] public DownloadSelect DownloadSelect
        {
            get => _downloadSelect;
            set
            {
                if (value != _downloadSelect)
                {
                    _downloadSelect = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore] public static string ProfileDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WirePix", "Profiles");
        public void Save(SaveOptions options = null) => new ProfileStore(ProfileDirectory).Save(SaveOptions.FileName, this, options);
        public bool Load(string profileName) => new ProfileStore(ProfileDirectory).Load(profileName, this);
        public bool IsValid(string profileName) => new ProfileStore(ProfileDirectory).IsValid(profileName);
        public static void Delete(string profileName) => new ProfileStore(ProfileDirectory).Delete(profileName);
    }

    public sealed class DeviceInfo : ObservableObject
    {
        private string _name = string.Empty, _originalName = string.Empty;
        private bool _connected;
        private DateTime _lastBackup;

        public DeviceInfo()
        {
        }

        public DeviceInfo(string name, string id, DateTime lastBackup = default, bool connected = false)
        {
            _originalName = _name = name;
            ID = id;
            LastBackup = lastBackup;
            _connected = connected;
        }

        public string ID { get; set; }

        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged();
                    if (_originalName == string.Empty) _originalName = _name;
                }
            }
        }

        public DateTime LastBackup
        {
            get => _lastBackup;
            set
            {
                if (value != _lastBackup)
                {
                    _lastBackup = value;
                    OnPropertyChanged();
                }
            }
        }

        [XmlIgnore] public bool Connected
        {
            get => _connected;
            set
            {
                if (value != _connected)
                {
                    _connected = value;
                    OnPropertyChanged();
                    if (!_connected && _name == string.Empty) _name = _originalName;
                }
            }
        }
    }
}