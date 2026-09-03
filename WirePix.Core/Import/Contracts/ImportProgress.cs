using System;

namespace WirePix.Core.Import.Contracts;

public readonly struct ImportProgress(
    ImportStage stage,
    int completed,
    int total,
    string currentFile = null,
    long bytesCompleted = 0,
    long bytesTotal = 0,
    TimeSpan estimatedRemaining = default)
{
    public ImportStage Stage { get; } = stage;
    public int Completed { get; } = completed;
    public int Total { get; } = total;
    public string CurrentFile { get; } = currentFile;
    public long BytesCompleted { get; } = bytesCompleted;
    public long BytesTotal { get; } = bytesTotal;
    public TimeSpan EstimatedRemaining { get; } = estimatedRemaining;
}
