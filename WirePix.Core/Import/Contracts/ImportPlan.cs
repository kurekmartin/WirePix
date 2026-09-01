using System.Collections.Generic;
using WirePix.Core.Models.Settings;

namespace WirePix.Core.Import.Contracts;

public sealed class ImportPlan
{
    public IReadOnlyList<ImportFile> Files { get; init; }
    public long TotalBytes { get; init; }
    public DateRange Date { get; init; }
}
