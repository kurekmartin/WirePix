namespace WirePix.Core.Import.Contracts;

public sealed class ImportResult
{
    public int FilesDone { get; internal set; }
    public int FilesTotal { get; internal set; }
    public int Errors { get; internal set; }
    public int Deleted { get; internal set; }
}
