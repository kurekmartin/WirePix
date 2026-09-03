using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using WirePix.Core.Models.Settings;

namespace WirePix.Core.Storage;

public sealed class ProfileStore(string directory)
{
    private readonly string _directory = directory ?? throw new ArgumentNullException(paramName: nameof(directory));

    public void Save(string profileName, DownloadSettings settings, SaveOptions options = null)
    {
        settings.SaveOptions = options ?? settings.SaveOptions;
        Directory.CreateDirectory(path: _directory);
        var ignore = new XmlAttributes
        {
            XmlIgnore = true
        };

        var overrides = new XmlAttributeOverrides();
        if (!settings.SaveOptions.Root)
        {
            overrides.Add(type: typeof(PathStruct), member: nameof(PathStruct.Root), attributes: ignore);
        }

        if (!settings.SaveOptions.FolderStruct)
        {
            overrides.Add(type: typeof(PathStruct), member: nameof(PathStruct.FolderTags), attributes: ignore);
        }

        if (!settings.SaveOptions.FileStruct)
        {
            overrides.Add(type: typeof(PathStruct), member: nameof(PathStruct.FileTags), attributes: ignore);
        }

        if (!settings.SaveOptions.Backup)
        {
            overrides.Add(type: typeof(DownloadSettings), member: nameof(DownloadSettings.Backup), attributes: ignore);
            overrides.Add(type: typeof(PathStruct), member: nameof(PathStruct.Backup), attributes: ignore);
        }

        if (!settings.SaveOptions.Thumbnails)
        {
            overrides.Add(type: typeof(PathStruct), member: nameof(PathStruct.Thumbnail), attributes: ignore);
            overrides.Add(type: typeof(DownloadSettings), member: nameof(DownloadSettings.Thumbnail), attributes: ignore);
            overrides.Add(type: typeof(DownloadSettings), member: nameof(DownloadSettings.ThumbnailSettings), attributes: ignore);
        }

        if (!settings.SaveOptions.FileCheck)
        {
            overrides.Add(type: typeof(DownloadSettings), member: nameof(DownloadSettings.CheckFiles), attributes: ignore);
        }

        if (!settings.SaveOptions.DeleteFiles)
        {
            overrides.Add(type: typeof(DownloadSettings), member: nameof(DownloadSettings.DeleteFiles), attributes: ignore);
        }

        var serializer = new XmlSerializer(type: typeof(DownloadSettings), overrides: overrides);
        using StreamWriter writer = File.CreateText(path: Path.Combine(path1: _directory, path2: profileName + ".xml"));
        serializer.Serialize(textWriter: writer, o: settings);
    }

    public bool Load(string profileName, DownloadSettings target)
    {
        string path = Path.Combine(path1: _directory, path2: profileName + ".xml");
        if (!File.Exists(path: path) || !IsValid(profileName: profileName))
        {
            return false;
        }

        try
        {
            var serializer = new XmlSerializer(type: typeof(DownloadSettings));
            using FileStream stream = File.OpenRead(path: path);
            var loaded = (DownloadSettings)serializer.Deserialize(stream: stream);
            if (loaded is not null)
            {
                if (loaded.SaveOptions.Root)
                {
                    target.Paths.Root = loaded.Paths.Root;
                }

                if (loaded.SaveOptions.FolderStruct)
                {
                    target.Paths.FolderTags = loaded.Paths.FolderTags;
                }

                if (loaded.SaveOptions.FileStruct)
                {
                    target.Paths.FileTags = loaded.Paths.FileTags;
                }

                if (loaded.SaveOptions.Backup)
                {
                    target.Paths.Backup = loaded.Paths.Backup;
                    target.Backup = loaded.Backup;
                }

                if (loaded.SaveOptions.Thumbnails)
                {
                    target.Paths.Thumbnail = loaded.Paths.Thumbnail;
                    target.Thumbnail = loaded.Thumbnail;
                    target.ThumbnailSettings = loaded.ThumbnailSettings;
                }

                if (loaded.SaveOptions.FileCheck)
                {
                    target.CheckFiles = loaded.CheckFiles;
                }

                if (loaded.SaveOptions.DeleteFiles)
                {
                    target.DeleteFiles = loaded.DeleteFiles;
                }

                target.SaveOptions = loaded.SaveOptions;
            }

            target.SaveOptions.FileName = profileName;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsValid(string profileName)
    {
        string path = Path.Combine(path1: _directory, path2: profileName + ".xml");
        if (!File.Exists(path: path))
        {
            return false;
        }

        try
        {
            using FileStream stream = File.OpenRead(path: path);
            using var reader = XmlReader.Create(input: stream);
            return new XmlSerializer(type: typeof(DownloadSettings)).CanDeserialize(xmlReader: reader);
        }
        catch
        {
            return false;
        }
    }

    public void Delete(string profileName)
    {
        string path = Path.Combine(path1: _directory, path2: profileName + ".xml");
        if (File.Exists(path: path))
        {
            File.Delete(path: path);
        }
    }

    public string[] GetProfiles()
    {
        return Directory.Exists(path: _directory) ? Directory.GetFiles(path: _directory, searchPattern: "*.xml") : [];
    }

    public string[] GetProfileNames()
    {
        return GetProfiles()
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name) && IsValid(name))
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}
