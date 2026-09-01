using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using WirePix.Core.Models;

namespace WirePix.Core.Storage
{
    public sealed class ProfileStore
    {
        private readonly string _directory;

        public ProfileStore(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public void Save(string profileName, DownloadSettings settings, SaveOptions options = null)
        {
            settings.SaveOptions = options ?? settings.SaveOptions;
            Directory.CreateDirectory(_directory);
            var ignore = new XmlAttributes();
            ignore.XmlIgnore = true;
            var overrides = new XmlAttributeOverrides();
            if (!settings.SaveOptions.Root)
            {
                overrides.Add(typeof(PathStruct), nameof(PathStruct.Root), ignore);
            }

            if (!settings.SaveOptions.FolderStruct)
            {
                overrides.Add(typeof(PathStruct), nameof(PathStruct.FolderTags), ignore);
            }

            if (!settings.SaveOptions.FileStruct)
            {
                overrides.Add(typeof(PathStruct), nameof(PathStruct.FileTags), ignore);
            }

            if (!settings.SaveOptions.Backup)
            {
                overrides.Add(typeof(DownloadSettings), nameof(DownloadSettings.Backup), ignore);
                overrides.Add(typeof(PathStruct), nameof(PathStruct.Backup), ignore);
            }

            if (!settings.SaveOptions.Thumbnails)
            {
                overrides.Add(typeof(PathStruct), nameof(PathStruct.Thumbnail), ignore);
                overrides.Add(typeof(DownloadSettings), nameof(DownloadSettings.Thumbnail), ignore);
                overrides.Add(typeof(DownloadSettings), nameof(DownloadSettings.ThumbnailSettings), ignore);
            }

            if (!settings.SaveOptions.FileCheck)
            {
                overrides.Add(typeof(DownloadSettings), nameof(DownloadSettings.CheckFiles), ignore);
            }

            if (!settings.SaveOptions.DeleteFiles)
            {
                overrides.Add(typeof(DownloadSettings), nameof(DownloadSettings.DeleteFiles), ignore);
            }

            var serializer = new XmlSerializer(typeof(DownloadSettings), overrides);
            using var writer = File.CreateText(Path.Combine(_directory, profileName + ".xml"));
            serializer.Serialize(writer, settings);
        }

        public bool Load(string profileName, DownloadSettings target)
        {
            var path = Path.Combine(_directory, profileName + ".xml");
            if (!File.Exists(path) || !IsValid(profileName))
            {
                return false;
            }

            try
            {
                var serializer = new XmlSerializer(typeof(DownloadSettings));
                using var stream = File.OpenRead(path);
                var loaded = (DownloadSettings)serializer.Deserialize(stream);
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
            var path = Path.Combine(_directory, profileName + ".xml");
            if (!File.Exists(path))
            {
                return false;
            }

            try
            {
                using var stream = File.OpenRead(path);
                using var reader = XmlReader.Create(stream);
                return new XmlSerializer(typeof(DownloadSettings)).CanDeserialize(reader);
            }
            catch
            {
                return false;
            }
        }

        public void Delete(string profileName)
        {
            var path = Path.Combine(_directory, profileName + ".xml");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public string[] GetProfiles() => Directory.Exists(_directory) ? Directory.GetFiles(_directory, "*.xml") : Array.Empty<string>();
    }
}