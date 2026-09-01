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
    } = DownloadSelect.LastBackup;

    [XmlIgnore]
    public static string ProfileDirectory { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WirePix", "Profiles");

    public void Save(SaveOptions options = null) => new ProfileStore(ProfileDirectory).Save(SaveOptions.FileName, this, options);
    public bool Load(string profileName) => new ProfileStore(ProfileDirectory).Load(profileName, this);
    public static bool IsValid(string profileName) => new ProfileStore(ProfileDirectory).IsValid(profileName);
    public static void Delete(string profileName) => new ProfileStore(ProfileDirectory).Delete(profileName);
}