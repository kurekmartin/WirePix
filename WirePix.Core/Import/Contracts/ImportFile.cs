using System;

namespace WirePix.Core.Import.Contracts;

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