using System;
using System.IO;
using ImageMagick;
using WirePix.Core.Models.Settings;

namespace WirePix.Core.Imaging;

public sealed class ThumbnailGenerator
{
    public static void Generate(string source, string output, Thumbnails settings, string hash = null)
    {
        if (settings is not { Value: > 0 })
        {
            return;
        }

        Directory.CreateDirectory(path: Path.GetDirectoryName(path: output) ?? string.Empty);
        using var image = new MagickImage(fileName: source);
        var geometry = new MagickGeometry(widthAndHeight: (uint)settings.Value) { FillArea = settings.Selected == ThumbnailSelect.ShorterSide };
        image.AutoOrient();
        image.Thumbnail(geometry: geometry);
        if (hash != null)
        {
            image.Comment = hash;
        }

        image.Write(fileName: output, format: MagickFormat.Jpg);
    }

    public static bool IsImage(string path)
    {
        try
        {
            IMagickFormatInfo format = MagickFormatInfo.Create(fileName: path);
            return format != null &&
                   (format.ModuleFormat == MagickFormat.Dng ||
                    (format.MimeType?.Contains(value: "image", comparisonType: StringComparison.OrdinalIgnoreCase) ?? false));
        }
        catch
        {
            return false;
        }
    }
}