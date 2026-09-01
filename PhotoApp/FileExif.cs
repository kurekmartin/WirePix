using System;

namespace PhotoApp;

internal static class FileExif
{
    public static DateTime GetDateTimeOriginal(string path)
    {
        return WirePix.Core.Metadata.FileExif.GetDateTimeOriginal(path: path);
    }

    public static string GetManufacturer(string path)
    {
        return WirePix.Core.Metadata.FileExif.GetManufacturer(path: path);
    }

    public static string GetModel(string path)
    {
        return WirePix.Core.Metadata.FileExif.GetModel(path: path);
    }
}