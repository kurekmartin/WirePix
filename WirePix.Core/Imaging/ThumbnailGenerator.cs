using System;
using System.IO;
using ImageMagick;
using WirePix.Core.Models.Settings;

namespace WirePix.Core.Imaging
{
    public sealed class ThumbnailGenerator
    {
        public static void Generate(string source, string output, Thumbnails settings, string hash = null)
        {
            if (settings is not { Value: > 0 })
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? string.Empty);
            using var image = new MagickImage(source);
            var geometry = new MagickGeometry((uint)settings.Value) { FillArea = settings.Selected == ThumbnailSelect.ShorterSide };
            image.AutoOrient();
            image.Thumbnail(geometry);
            if (hash != null)
            {
                image.Comment = hash;
            }

            image.Write(output, MagickFormat.Jpg);
        }

        public static bool IsImage(string path)
        {
            try
            {
                IMagickFormatInfo format = MagickFormatInfo.Create(path);
                return format != null &&
                       (format.ModuleFormat == MagickFormat.Dng ||
                        (format.MimeType?.Contains("image", StringComparison.OrdinalIgnoreCase) ?? false));
            }
            catch
            {
                return false;
            }
        }
    }
}
