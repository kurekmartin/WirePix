namespace WirePix.Core.Import.Contracts;

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