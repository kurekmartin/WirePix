using MediaDevices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WirePix.Core.Import;

namespace PhotoApp.Devices
{
    /// <summary>Windows-only transport adapter. Core never references MediaDevices.</summary>
    public sealed class MediaDevicesImportDevice : IImportDevice
    {
        private readonly MediaDevice _device;
        public MediaDevicesImportDevice(MediaDevice device) { _device = device ?? throw new ArgumentNullException(nameof(device)); }
        public string Id => _device.DeviceId;
        public string Name => _device.Description ?? string.Empty;
        public string Manufacturer => _device.Manufacturer ?? string.Empty;
        public bool IsConnected => _device.IsConnected;
        public double[] GetSpace()
        {
            _device.Connect();
            double total = 0, free = 0;
            foreach (var drive in _device.GetDrives()) { total += drive.TotalSize / 1073741824d; free += drive.AvailableFreeSpace / 1073741824d; }
            _device.Disconnect();
            return new[] { Math.Round(total, 2), Math.Round(free, 2), Math.Round(total - free, 2) };
        }
        public void Connect() => _device.Connect();
        public void Disconnect() { if (_device.IsConnected) _device.Disconnect(); }
        public IReadOnlyList<ImportDrive> GetDrives() => _device.GetDrives().Select(x => new ImportDrive { RootPath = x.RootDirectory?.FullName }).Where(x => x.RootPath != null).ToList();
        public IEnumerable<string> EnumerateDirectories(string path) => _device.EnumerateDirectories(path);
        public IEnumerable<ImportFile> EnumerateFiles(string path) => _device.GetDirectoryInfo(path).EnumerateFiles("*", SearchOption.TopDirectoryOnly).Select(x => new ImportFile
        {
            PersistentId = x.PersistentUniqueId, FullName = x.FullName, Name = x.Name, Length = checked((long)x.Length),
            CreationTime = x.CreationTime ?? default, DateAuthored = x.DateAuthored ?? default, LastWriteTime = x.LastWriteTime ?? default
        });
        public Stream OpenRead(ImportFile file) => _device.GetFileInfo(file.FullName).OpenRead();
        public void Download(ImportFile file, string destination) => _device.DownloadFileFromPersistentUniqueId(file.PersistentId, destination);
        public void Delete(string fullName) => _device.DeleteFile(fullName);
        public bool FileExists(string fullName) => _device.FileExists(fullName);
    }
}
