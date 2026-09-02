using System.Collections.Generic;
using System.Linq;
using WirePix.Core.Naming.Templates;

namespace WirePix.Core.Naming.Tokens;

public static class TagCatalog
{
    public const string YearGroup = "Year";
    public const string MonthGroup = "Month";
    public const string DayGroup = "Day";
    public const string SeparatorGroup = "Separator";

    public static IReadOnlyList<TagDefinition> All { get; } =
    [
        new(NamingTokens.YearLong, YearGroup),
        new(NamingTokens.Year, YearGroup),
        new(NamingTokens.Month, MonthGroup),
        new(NamingTokens.MonthShort, MonthGroup),
        new(NamingTokens.MonthLong, MonthGroup),
        new(NamingTokens.Day, DayGroup),
        new(NamingTokens.DayShort, DayGroup),
        new(NamingTokens.DayLong, DayGroup),
        new(NamingTokens.DeviceName),
        new(NamingTokens.DeviceManufacturer),
        new(NamingTokens.Sequence),
        new(NamingTokens.CustomText),
        new(NamingTokens.FileName),
        new(NamingTokens.NewFolder),
        new(NamingTokens.Hyphen, SeparatorGroup),
        new(NamingTokens.Underscore, SeparatorGroup)
    ];

    public static TagDefinition Get(string code)
    {
        string normalized = NameTemplate.NormalizeCode(code: code);
        return All.FirstOrDefault(predicate: x => x.Code == normalized);
    }

    public static IReadOnlyList<TagDefinition> GetGroup(string group)
    {
        return All.Where(predicate: x => x.Group == group).ToList();
    }
}
