using System.Globalization;
using WirePix.Devices.Models;

namespace WirePix.Core.Naming.Templates;

public sealed class NamingContext
{
    public MediaItem File { get; init; }
    public string LocalFilePath { get; init; }
    public string DeviceName { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public CultureInfo Culture { get; init; } = CultureInfo.CurrentUICulture;
    public string TagLanguage { get; init; }
    public bool UseTagLanguage { get; init; }
}
