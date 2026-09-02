using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WirePix.Core.Metadata;
using WirePix.Core.Naming.Tokens;
using WirePix.Devices.Models;

namespace WirePix.Core.Naming.Templates;

public static partial class NameTemplate
{
    public static List<string> Parse(string template)
    {
        var result = new List<string>();
        while (!string.IsNullOrEmpty(value: template))
        {
            Match match = TokenRegex().Match(input: template);
            string token = match.Success ? match.Value : FreeTextRegex().Match(input: template).Value;
            if (match.Success && token.Length < template.Length && template[index: token.Length] == '(')
            {
                token += TagParameterRegex().Match(input: template).Value;
            }

            if (token.Length == 0)
            {
                break;
            }

            result.Add(item: token);
            template = template[token.Length..];
        }

        return result;
    }

    public static string Evaluate(IEnumerable<string> tags, NamingContext context)
    {
        if (context == null)
        {
            return string.Empty;
        }

        MediaItem file = context.File;
        DateTime date = string.IsNullOrEmpty(value: context.LocalFilePath)
            ? default
            : FileExif.GetDateTimeOriginal(path: context.LocalFilePath);
        if (date == default)
        {
            date = file?.CapturedAt?.LocalDateTime ?? default;
        }

        CultureInfo culture = ResolveDateCulture(context);
        string manufacturer = string.IsNullOrEmpty(value: context.LocalFilePath)
            ? string.Empty
            : FileExif.GetManufacturer(path: context.LocalFilePath);
        string model = string.IsNullOrEmpty(value: context.LocalFilePath)
            ? string.Empty
            : FileExif.GetModel(path: context.LocalFilePath);
        var values = new List<string>();
        foreach (string tag in tags ?? [])
        {
            string code = NormalizeCode(code: RemoveParameter(tag: tag));
            switch (code)
            {
                case NamingTokens.CustomText: values.Add(item: GetParameter(tag: tag)); break;
                case NamingTokens.YearLong: values.Add(item: date.Year.ToString(provider: culture)); break;
                case NamingTokens.Year: values.Add(item: (date.Year % 100).ToString(provider: culture)); break;
                case NamingTokens.Month: values.Add(item: date.Month.ToString(format: "00", provider: culture)); break;
                case NamingTokens.MonthShort: values.Add(item: culture.DateTimeFormat.GetAbbreviatedMonthName(month: date.Month)); break;
                case NamingTokens.MonthLong: values.Add(item: culture.DateTimeFormat.GetMonthName(month: date.Month)); break;
                case NamingTokens.Day: values.Add(item: date.Day.ToString(format: "00", provider: culture)); break;
                case NamingTokens.DayShort: values.Add(item: culture.DateTimeFormat.GetAbbreviatedDayName(dayofweek: date.DayOfWeek)); break;
                case NamingTokens.DayLong: values.Add(item: culture.DateTimeFormat.GetDayName(dayofweek: date.DayOfWeek)); break;
                case NamingTokens.DeviceName: values.Add(item: string.IsNullOrEmpty(value: model) ? context.DeviceName ?? string.Empty : model); break;
                case NamingTokens.DeviceManufacturer: values.Add(item: string.IsNullOrEmpty(value: manufacturer) ? context.Manufacturer ?? string.Empty : manufacturer); break;
                case NamingTokens.FileName: values.Add(item: file == null ? string.Empty : Path.GetFileNameWithoutExtension(path: file.FileName)); break;
                case NamingTokens.Sequence: values.Add(item: "####"); break;
                case NamingTokens.NewFolder: values.Add(item: Path.DirectorySeparatorChar.ToString()); break;
                case NamingTokens.Hyphen: values.Add(item: "-"); break;
                case NamingTokens.Underscore: values.Add(item: "_"); break;
                default: values.Add(item: tag); break;
            }
        }

        return string.Concat(values: values);
    }

    private static CultureInfo ResolveDateCulture(NamingContext context)
    {
        CultureInfo fallback = context.Culture ?? CultureInfo.CurrentUICulture;
        if (!context.UseTagLanguage || string.IsNullOrWhiteSpace(value: context.TagLanguage))
        {
            return fallback;
        }

        if (string.Equals(
                a: context.TagLanguage,
                b: "system",
                comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return CultureInfo.CurrentCulture;
        }

        try
        {
            return CultureInfo.CreateSpecificCulture(name: context.TagLanguage);
        }
        catch (CultureNotFoundException)
        {
            return fallback;
        }
    }

    private static string NormalizeCode(string code)
    {
        return code is ['{', _, ..] && code[^1] == '}'
            ? code[1..^1]
            : code;
    }

    public static string EvaluateFolders(IEnumerable<IEnumerable<string>> folders, NamingContext context)
    {
        return Path.Combine(paths: (folders ?? [])
                                   .Select(selector: x => Evaluate(tags: x, context: context))
                                   .ToArray());
    }

    public static string RemoveParameter(string tag)
    {
        return string.Join(separator: string.Empty, values: (tag ?? string.Empty)
            .TakeWhile(predicate: c => c != '('));
    }

    public static string GetParameter(string tag)
    {
        return TagParameterContentRegex().Match(input: tag ?? string.Empty).Value;
    }

    public static bool IsValidFileName(string text)
    {
        return !string.IsNullOrEmpty(value: text) && text.IndexOfAny(anyOf: Path.GetInvalidFileNameChars()) < 0;
    }

    [GeneratedRegex(pattern: @"\{.*?\}")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(pattern: @"[^\{]*")]
    private static partial Regex FreeTextRegex();

    [GeneratedRegex(pattern: @"\(.*?\)")]
    private static partial Regex TagParameterRegex();

    [GeneratedRegex(pattern: @"(?<=\().+?(?=\))")]
    private static partial Regex TagParameterContentRegex();
}
