using System.Globalization;
using WirePix.Core.Import.Contracts;

namespace WirePix.Core.Naming.Templates;

public sealed class NamingContext
{
    public ImportFile File { get; init; }
    public string DeviceName { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public CultureInfo Culture { get; init; } = CultureInfo.CurrentCulture;
    public string TagLanguage { get; init; }
    public bool UseTagLanguage { get; init; }
}
