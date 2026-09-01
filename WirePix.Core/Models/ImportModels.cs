using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using WirePix.Core.Storage;

namespace WirePix.Core.Models
{
    public enum DownloadSelect
    {
        LastBackup,
        DateRange
    }

    public enum ThumbnailSelect
    {
        LongerSide,
        ShorterSide
    }

    public sealed class PathStruct : ObservableObject
    {
        private string _root = string.Empty, _backup = string.Empty, _thumbnail = string.Empty;
        private List<List<string>> _folderTags = [];
        private List<string> _fileTags = [];

        public string Root
        {
            get => _root;
            set
            {
                if (value == _root)
                {
                    return;
                }

                _root = value;
                OnPropertyChanged();
            }
        }

        public List<List<string>> FolderTags
        {
            get => _folderTags;
            set
            {
                if (value == _folderTags)
                {
                    return;
                }

                _folderTags = value;
                OnPropertyChanged();
            }
        }

        public List<string> FileTags
        {
            get => _fileTags;
            set
            {
                if (value == _fileTags)
                {
                    return;
                }

                _fileTags = value;
                OnPropertyChanged();
            }
        }

        public string Backup
        {
            get => _backup;
            set
            {
                if (value == _backup)
                {
                    return;
                }

                _backup = value;
                OnPropertyChanged();
            }
        }

        public string Thumbnail
        {
            get => _thumbnail;
            set
            {
                if (value == _thumbnail)
                {
                    return;
                }

                _thumbnail = value;
                OnPropertyChanged();
            }
        }
    }

    public sealed class DateRange : ObservableObject
    {
        public DateRange()
        {
            Start = End = DateTime.Now.Date;
        }

        public DateTime Start
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

        public DateTime End
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

        public static bool operator ==(DateRange a, DateRange b) => ReferenceEquals(a, b) || (a != null && b != null && a.Start == b.Start && a.End == b.End);
        public static bool operator !=(DateRange a, DateRange b) => !(a == b);
        public override bool Equals(object obj) => obj is DateRange other && this == other;
        public override int GetHashCode() => HashCode.Combine(Start, End);
    }

    public sealed class Thumbnails : ObservableObject
    {
        public ThumbnailSelect Selected
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

        public int Value
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
    }

    public sealed class SaveOptions : ObservableObject
    {
        [XmlIgnore] public string FileName
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
        } = string.Empty;

        public bool Root
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

        public bool FolderStruct
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

        public bool FileStruct
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

        public bool Backup
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

        public bool Thumbnails
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

        public bool FileCheck
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

        public bool DeleteFiles
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

        public SaveOptions Copy() => (SaveOptions)MemberwiseClone();
    }

    public sealed class DownloadSettings : ObservableObject
    {
        public PathStruct Paths { get; set; } = new();
        [XmlIgnore] public DateRange Date { get; set; } = new();
        public Thumbnails ThumbnailSettings { get; set; } = new();
        public SaveOptions SaveOptions = new();

        public bool CheckFiles
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

        public bool DeleteFiles
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

        public bool Backup
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

        public bool Thumbnail
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

        [XmlIgnore] public DownloadSelect DownloadSelect
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
        } = DownloadSelect.LastBackup;

        [XmlIgnore]
        public static string ProfileDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WirePix", "Profiles");

        public void Save(SaveOptions options = null) => new ProfileStore(ProfileDirectory).Save(SaveOptions.FileName, this, options);
        public bool Load(string profileName) => new ProfileStore(ProfileDirectory).Load(profileName, this);
        public static bool IsValid(string profileName) => new ProfileStore(ProfileDirectory).IsValid(profileName);
        public static void Delete(string profileName) => new ProfileStore(ProfileDirectory).Delete(profileName);
    }

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
}