using System;

namespace PhotoApp;

internal static class FileExif
{
    public static DateTime GetDateTimeOriginal(string path) => WirePix.Core.Metadata.FileExif.GetDateTimeOriginal(path);
    public static string GetManufacturer(string path) => WirePix.Core.Metadata.FileExif.GetManufacturer(path);
    public static string GetModel(string path) => WirePix.Core.Metadata.FileExif.GetModel(path);
}