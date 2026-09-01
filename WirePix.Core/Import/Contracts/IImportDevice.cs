using System.Collections.Generic;
using System.IO;

namespace WirePix.Core.Import.Contracts;

public interface IImportDevice
{
    string Id { get; }
    string Name { get; }
    string Manufacturer { get; }
    bool IsConnected { get; }
    void Connect();
    void Disconnect();
    IReadOnlyList<ImportDrive> GetDrives();
    IEnumerable<string> EnumerateDirectories(string path);
    IEnumerable<ImportFile> EnumerateFiles(string path);
    Stream OpenRead(ImportFile file);
    void Download(ImportFile file, string destination);
    void Delete(string fullName);
    bool FileExists(string fullName);
}