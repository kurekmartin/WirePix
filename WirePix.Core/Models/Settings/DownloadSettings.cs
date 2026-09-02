using System;
using System.IO;
using System.Xml.Serialization;
using WirePix.Core.Storage;

namespace WirePix.Core.Models.Settings;

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
    } = DownloadSelect.lastBackup;

    [XmlIgnore]
    public static string ProfileDirectory { get; set; } = Path.Combine(path1: Environment.GetFolderPath(folder: Environment.SpecialFolder.ApplicationData), path2: "WirePix", path3: "Profiles");

    public void Save(SaveOptions options = null)
    {
        new ProfileStore(directory: ProfileDirectory).Save(profileName: SaveOptions.FileName, settings: this, options: options);
    }

    public bool Load(string profileName)
    {
        return new ProfileStore(directory: ProfileDirectory).Load(profileName: profileName, target: this);
    }

    public static bool IsValid(string profileName)
    {
        return new ProfileStore(directory: ProfileDirectory).IsValid(profileName: profileName);
    }

    public static void Delete(string profileName)
    {
        new ProfileStore(directory: ProfileDirectory).Delete(profileName: profileName);
    }
}