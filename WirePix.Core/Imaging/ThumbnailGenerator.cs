using System;
using System.IO;
using ImageMagick;
using ImageMagick.Formats;
using WirePix.Core.Files;
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

        output = ResolveOutput(source: source, output: output, settings: settings, hash: hash);
        if (output == null)
        {
            return;
        }

        var geometry = new MagickGeometry(widthAndHeight: (uint)settings.Value)
        {
            FillArea = settings.Selected == ThumbnailSelect.shorterSide
        };
        var readSettings = new MagickReadSettings
        {
            Defines = new DngReadDefines
            {
                ReadThumbnail = true,
                UseCameraWhiteBalance = true
            }
        };

        var thumbnailCreated = false;
        using (var image = new MagickImage())
        {
            image.Ping(fileName: source, readSettings: readSettings);
            IImageProfile profile = image.GetProfile(name: "dng:thumbnail");
            if (profile != null)
            {
                using var embeddedThumbnail = new MagickImage(data: profile.ToByteArray());
                if (IsLargerResolution(
                        settings: settings,
                        width: embeddedThumbnail.Width,
                        height: embeddedThumbnail.Height))
                {
                    embeddedThumbnail.AutoOrient();
                    embeddedThumbnail.Thumbnail(geometry: geometry);
                    CreateOutputDirectory(output: output);
                    embeddedThumbnail.Write(fileName: output);
                    thumbnailCreated = true;
                }
            }
        }

        if (!thumbnailCreated)
        {
            using var image = new MagickImage(fileName: source, readSettings: readSettings);
            image.Thumbnail(geometry: geometry);
            image.TransformColorSpace(target: ColorProfiles.AdobeRGB1998);
            image.AutoLevel();
            image.Comment = hash;
            CreateOutputDirectory(output: output);
            image.Write(fileName: output, format: MagickFormat.Jpg);
        }
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

    private static string ResolveOutput(string source, string output, Thumbnails settings, string hash)
    {
        if (!File.Exists(path: output))
        {
            return output;
        }

        using var image = new MagickImage(fileName: output);
        if (image.Comment == hash)
        {
            bool matches = settings.Selected == ThumbnailSelect.longerSide
                ? settings.Value == Math.Max(val1: image.Width, val2: image.Height)
                : settings.Value == Math.Min(val1: image.Width, val2: image.Height);
            return matches ? null : output;
        }

        return FileOperations.UniqueName(path: output, sourcePath: source, alwaysUnique: true);
    }

    private static bool IsLargerResolution(Thumbnails settings, uint width, uint height)
    {
        return settings.Selected == ThumbnailSelect.longerSide
            ? Math.Max(val1: width, val2: height) > settings.Value
            : Math.Min(val1: width, val2: height) > settings.Value;
    }

    private static void CreateOutputDirectory(string output)
    {
        string directory = Path.GetDirectoryName(path: output);
        if (!string.IsNullOrEmpty(value: directory))
        {
            Directory.CreateDirectory(path: directory);
        }
    }
}
