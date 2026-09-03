using System;

namespace WirePix.Core.Units;

/// <summary>
/// Converts byte counts to larger binary units.
/// </summary>
public static class ByteSizeConverter
{
    private const double BytesPerKilobyte = 1024d;
    private const double BytesPerMegabyte = BytesPerKilobyte * 1024d;
    private const double BytesPerGigabyte = BytesPerMegabyte * 1024d;

    public static double ToKilobytes(long bytes, int decimalPlaces = 2)
    {
        return Convert(bytes, BytesPerKilobyte, decimalPlaces);
    }

    public static double ToMegabytes(long bytes, int decimalPlaces = 2)
    {
        return Convert(bytes, BytesPerMegabyte, decimalPlaces);
    }

    public static double ToGigabytes(long bytes, int decimalPlaces = 2)
    {
        return Convert(bytes, BytesPerGigabyte, decimalPlaces);
    }

    private static double Convert(long bytes, double bytesPerUnit, int decimalPlaces)
    {
        return Math.Round(bytes / bytesPerUnit, decimalPlaces);
    }
}
