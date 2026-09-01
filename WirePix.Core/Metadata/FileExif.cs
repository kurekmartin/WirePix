using System;
using System.Globalization;
using System.Linq;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace WirePix.Core.Metadata
{
    public static class FileExif
    {
        public static DateTime GetDateTimeOriginal(string path)
        {
            try
            {
                ExifSubIfdDirectory directory = ImageMetadataReader.ReadMetadata(path)
                                                                   .OfType<ExifSubIfdDirectory>()
                                                                   .FirstOrDefault();
                string value = directory?.GetDescription(ExifDirectoryBase.TagDateTimeOriginal);
                return value == null ? default : DateTime.ParseExact(value, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            catch
            {
                return default;
            }
        }

        public static string GetManufacturer(string path) => GetDescription(path, d => d.GetDescription(ExifDirectoryBase.TagMake));
        public static string GetModel(string path) => GetDescription(path, d => d.GetDescription(ExifDirectoryBase.TagModel));

        private static string GetDescription(string path, Func<ExifIfd0Directory, string> selector)
        {
            try
            {
                return selector(ImageMetadataReader.ReadMetadata(path).OfType<ExifIfd0Directory>().FirstOrDefault());
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}