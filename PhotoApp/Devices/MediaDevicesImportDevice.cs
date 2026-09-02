using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaDevices;
using WirePix.Core.Import.Contracts;

namespace PhotoApp.Devices;

public sealed class MediaDevicesImportDevice(MediaDevice device) : IImportDevice
{
    private readonly MediaDevice _device = device ?? throw new ArgumentNullException(paramName: nameof(device));

    public string Id => _device.DeviceId;
    public string Name => _device.Description ?? string.Empty;
    public string Manufacturer => _device.Manufacturer ?? string.Empty;
    public bool IsConnected => _device.IsConnected;

    public double[] GetSpace()
    {
        _device.Connect();
        double total = 0, free = 0;
        foreach (MediaDriveInfo drive in _device.GetDrives())
        {
            total += drive.TotalSize / 1073741824d;
            free += drive.AvailableFreeSpace / 1073741824d;
        }

        _device.Disconnect();
        return [Math.Round(value: total, digits: 2), Math.Round(value: free, digits: 2), Math.Round(value: total - free, digits: 2)];
    }

    public void Connect()
    {
        _device.Connect();
    }

    public void Disconnect()
    {
        if (_device.IsConnected)
        {
            _device.Disconnect();
        }
    }

    public IReadOnlyList<ImportDrive> GetDrives()
    {
        return
        [
            .. _device.GetDrives()
                      .Select(selector: x => new ImportDrive { RootPath = x.RootDirectory?.FullName })
                      .Where(predicate: x => x.RootPath != null)
        ];
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        return _device.EnumerateDirectories(path: path);
    }

    public IEnumerable<ImportFile> EnumerateFiles(string path)
    {
        return _device.GetDirectoryInfo(path: path)
                      .EnumerateFiles(searchPattern: "*")
                      .Select(selector: x => new ImportFile
                      {
                          PersistentId = x.PersistentUniqueId,
                          FullName = x.FullName,
                          Name = x.Name,
                          Length = checked((long)x.Length),
                          CreationTime = x.CreationTime ?? default, DateAuthored = x.DateAuthored ?? default, LastWriteTime = x.LastWriteTime ?? default
                      });
    }

    public Stream OpenRead(ImportFile file)
    {
        return _device.GetFileInfo(path: file.FullName)
                      .OpenRead();
    }

    public void Download(ImportFile file, string destination)
    {
        _device.DownloadFileFromPersistentUniqueId(persistentUniqueId: file.PersistentId, destination: destination);
    }

    public void Delete(string fullName)
    {
        _device.DeleteFile(path: fullName);
    }

    public bool FileExists(string fullName)
    {
        return _device.FileExists(path: fullName);
    }
}