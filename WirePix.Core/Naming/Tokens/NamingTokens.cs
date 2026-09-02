using System;
using System.Collections.Generic;

namespace WirePix.Core.Naming.Tokens;

public static class NamingTokens
{
    public const string YearLong = "YYYY";
    public const string Year = "YY";
    public const string Month = "MM";
    public const string MonthShort = "MMM";
    public const string MonthLong = "MMMM";
    public const string Day = "DD";
    public const string DayShort = "DDD";
    public const string DayLong = "DDDD";
    public const string DeviceName = "DN";
    public const string DeviceManufacturer = "DM";
    public const string Sequence = "SN";
    public const string CustomText = "STR";
    public const string FileName = "FN";
    public const string NewFolder = "NF";
    public const string Hyphen = "HYP";
    public const string Underscore = "UNDS";

    public static readonly IReadOnlySet<string> DateTokens = new HashSet<string>(comparer: StringComparer.Ordinal)
    {
        YearLong,
        Year,
        Month,
        MonthShort,
        MonthLong,
        Day,
        DayShort,
        DayLong
    };
}
