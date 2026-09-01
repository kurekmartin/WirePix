using System;
using System.Collections.Generic;
using System.IO;
using WirePix.Core.Models;

namespace WirePix.Core.Import
{
    public sealed class ImportFile
    {
        public string PersistentId { get; init; }
        public string FullName { get; init; }
        public string Name { get; init; }
        public long Length { get; init; }
        public DateTime CreationTime { get; init; }
        public DateTime DateAuthored { get; init; }
        public DateTime LastWriteTime { get; init; }
    }

    public sealed class ImportDrive
    {
        public string RootPath { get; init; }
    }

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

    public enum ImportStage
    {
        Searching,
        Filtering,
        Downloading,
        GeneratingThumbnail,
        Saving,
        Deleting,
        Completed
    }

    public readonly struct ImportProgress
    {
        public ImportProgress(ImportStage stage, int completed, int total, string currentFile = null, long bytesCompleted = 0, long bytesTotal = 0)
        {
            Stage = stage;
            Completed = completed;
            Total = total;
            CurrentFile = currentFile;
            BytesCompleted = bytesCompleted;
            BytesTotal = bytesTotal;
        }

        public ImportStage Stage { get; }
        public int Completed { get; }
        public int Total { get; }
        public string CurrentFile { get; }
        public long BytesCompleted { get; }
        public long BytesTotal { get; }
    }

    public sealed class ImportPlan
    {
        public IReadOnlyList<ImportFile> Files { get; init; }
        public long TotalBytes { get; init; }
        public DateRange Date { get; init; }
    }

    public sealed class ImportResult
    {
        public int FilesDone { get; internal set; }
        public int FilesTotal { get; internal set; }
        public int Errors { get; internal set; }
        public int Deleted { get; internal set; }
        public bool Cancelled { get; internal set; }
    }
}