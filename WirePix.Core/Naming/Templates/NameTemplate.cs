using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using WirePix.Core.Import.Contracts;
using WirePix.Core.Metadata;
using WirePix.Core.Naming.Tokens;

namespace WirePix.Core.Naming.Templates;

public static partial class NameTemplate
{
    public static List<string> Parse(string template)
    {
        var result = new List<string>();
        while (!string.IsNullOrEmpty(template))
        {
            Match match = TokenRegex().Match(template);
            string token = match.Success ? match.Value : FreeTextRegex().Match(template).Value;
            if (match.Success && token.Length < template.Length && template[token.Length] == '(')
            {
                token += TagParameterRegex().Match(template).Value;
            }

            if (token.Length == 0)
            {
                break;
            }

            result.Add(token);
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

        ImportFile file = context.File;
        DateTime date = file == null ? default : FileExif.GetDateTimeOriginal(file.FullName);
        if (date == default && file != null)
        {
            date = file.CreationTime != default ? file.CreationTime : file.DateAuthored != default ? file.DateAuthored : file.LastWriteTime;
        }

        CultureInfo culture = context.Culture ?? CultureInfo.CurrentCulture;
        var values = new List<string>();
        foreach (string tag in tags ?? [])
        {
            string code = RemoveParameter(tag);
            switch (code)
            {
                case NamingTokens.CustomText: values.Add(GetParameter(tag)); break;
                case NamingTokens.YearLong: values.Add(date.Year.ToString(culture)); break;
                case NamingTokens.Year: values.Add((date.Year % 100).ToString(culture)); break;
                case NamingTokens.Month: values.Add(date.Month.ToString("00", culture)); break;
                case NamingTokens.MonthShort: values.Add(culture.DateTimeFormat.GetAbbreviatedMonthName(date.Month)); break;
                case NamingTokens.MonthLong: values.Add(culture.DateTimeFormat.GetMonthName(date.Month)); break;
                case NamingTokens.Day: values.Add(date.Day.ToString("00", culture)); break;
                case NamingTokens.DayShort: values.Add(culture.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek)); break;
                case NamingTokens.DayLong: values.Add(culture.DateTimeFormat.GetDayName(date.DayOfWeek)); break;
                case NamingTokens.DeviceName: values.Add(string.IsNullOrEmpty(context.DeviceName) ? string.Empty : context.DeviceName); break;
                case NamingTokens.DeviceManufacturer: values.Add(string.IsNullOrEmpty(context.Manufacturer) ? string.Empty : context.Manufacturer); break;
                case NamingTokens.FileName: values.Add(file == null ? string.Empty : Path.GetFileNameWithoutExtension(file.Name)); break;
                case NamingTokens.Sequence: values.Add("####"); break;
                case NamingTokens.NewFolder: values.Add(Path.DirectorySeparatorChar.ToString()); break;
                case NamingTokens.Hyphen: values.Add("-"); break;
                case NamingTokens.Underscore: values.Add("_"); break;
                default: values.Add(tag); break;
            }
        }

        return string.Concat(values);
    }

    public static string EvaluateFolders(IEnumerable<IEnumerable<string>> folders, NamingContext context) => Path.Combine((folders ?? [])
                                                                                                                          .Select(x => Evaluate(x, context))
                                                                                                                          .ToArray());

    public static string RemoveParameter(string tag) => string.Join(string.Empty, (tag ?? string.Empty)
        .TakeWhile(c => c != '('));

    public static string GetParameter(string tag) => TagParameterContentRegex().Match(tag ?? string.Empty).Value;
    public static bool IsValidFileName(string text) => !string.IsNullOrEmpty(text) && text.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    [GeneratedRegex(@"\{.*?\}")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"[^\{]*")]
    private static partial Regex FreeTextRegex();

    [GeneratedRegex(@"\(.*?\)")]
    private static partial Regex TagParameterRegex();

    [GeneratedRegex(@"(?<=\().+?(?=\))")]
    private static partial Regex TagParameterContentRegex();
}