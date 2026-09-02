using System;
using System.Globalization;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace WirePix.Core.Metadata;

public static class FileExif
{
    public static DateTime GetDateTimeOriginal(string path)
    {
        try
        {
            ExifSubIfdDirectory directory = ImageMetadataReader.ReadMetadata(filePath: path)
                                                               .OfType<ExifSubIfdDirectory>()
                                                               .FirstOrDefault();
            string value = directory?.GetDescription(tagType: ExifDirectoryBase.TagDateTimeOriginal);
            return value == null ? default : DateTime.ParseExact(s: value, format: "yyyy:MM:dd HH:mm:ss", provider: CultureInfo.InvariantCulture);
        }
        catch
        {
            return default;
        }
    }

    public static string GetManufacturer(string path)
    {
        return GetDescription(path: path, selector: d => d.GetDescription(tagType: ExifDirectoryBase.TagMake));
    }

    public static string GetModel(string path)
    {
        return GetDescription(path: path, selector: d => d.GetDescription(tagType: ExifDirectoryBase.TagModel));
    }

    private static string GetDescription(string path, Func<ExifIfd0Directory, string> selector)
    {
        try
        {
            return selector(arg: ImageMetadataReader.ReadMetadata(filePath: path).OfType<ExifIfd0Directory>().FirstOrDefault());
        }
        catch
        {
            return string.Empty;
        }
    }
}