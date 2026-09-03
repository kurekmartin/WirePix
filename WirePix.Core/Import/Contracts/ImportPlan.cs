using System.Collections.Generic;
using WirePix.Core.Models.Settings;
using WirePix.Devices.Models;

namespace WirePix.Core.Import.Contracts;

public sealed class ImportPlan
{
    public int DiscoveredFilesTotal { get; init; }
    public IReadOnlyList<MediaItem> Files { get; init; }
    public long TotalBytes { get; init; }
    public DateRange Date { get; init; }
}
