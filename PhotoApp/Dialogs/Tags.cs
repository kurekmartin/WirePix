using MaterialDesignThemes.Wpf;
using WirePix.Devices.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp;

public readonly struct TagStruct
{
    public TagStruct(string code, string visibleText, string buttonLabel, string group = "")
    {
        Code = code;
        VisibleText = visibleText;
        ButtonLabel = buttonLabel;
        Group = group;
    }

    public string Code { get; }
    public string VisibleText { get; }
    public string ButtonLabel { get; }
    public string Group { get; }

    public static bool operator ==(TagStruct n1, TagStruct n2)
    {
        return n1.Code == n2.Code && n1.VisibleText == n2.VisibleText && n1.ButtonLabel == n2.ButtonLabel;
    }

    public static bool operator !=(TagStruct n1, TagStruct n2)
    {
        return n1.Code != n2.Code || n1.VisibleText != n2.VisibleText || n1.ButtonLabel != n2.ButtonLabel;
    }
}

public readonly struct ButtonStruct
{
    public ButtonStruct(string code)
    {
        TagStruct nameString = Tags.GetTag(code: code);
        btnText = nameString.ButtonLabel;
        insertValue = nameString.Code;
    }

    public string btnText { get; }
    public string insertValue { get; }
}

public readonly struct ButtonGroupStruct
{
    public ButtonGroupStruct(string groupName, List<string> btns, Point gridPos)
    {
        this.groupName = groupName;

        //vytvoreni tlacitek z tagu
        buttons = new List<ButtonStruct>();
        foreach (string btn in btns)
        {
            buttons.Add(item: new ButtonStruct(code: btn));
        }

        gridPosition = gridPos;
    }

    public string groupName { get; }
    public List<ButtonStruct> buttons { get; }
    public Point gridPosition { get; }
}

internal static class Tags
{
    private static readonly List<TagStruct> tagList = new()
    {
        //            code                            visible text                                         button label                                         groups
        new TagStruct(code: Properties.TagCodes.YearLong, visibleText: Properties.Resources.Tag_YearLong_VisibleText, buttonLabel: Properties.Resources.Tag_YearLong_Button, group: Properties.TagGroups.Year),
        new TagStruct(code: Properties.TagCodes.Year, visibleText: Properties.Resources.Tag_YearShort_VisibleText, buttonLabel: Properties.Resources.Tag_YearShort_Button, group: Properties.TagGroups.Year),
        new TagStruct(code: Properties.TagCodes.Month, visibleText: Properties.Resources.Tag_Month_VisibleText, buttonLabel: Properties.Resources.Tag_Month_Button, group: Properties.TagGroups.Month),
        new TagStruct(code: Properties.TagCodes.MonthShort, visibleText: Properties.Resources.Tag_MonthShort_VisibleText, buttonLabel: Properties.Resources.Tag_MonthShort_Button, group: Properties.TagGroups.Month),
        new TagStruct(code: Properties.TagCodes.MonthLong, visibleText: Properties.Resources.Tag_MonthLong_VisibleText, buttonLabel: Properties.Resources.Tag_MonthLong_Button, group: Properties.TagGroups.Month),
        new TagStruct(code: Properties.TagCodes.Day, visibleText: Properties.Resources.Tag_Day_VisibleText, buttonLabel: Properties.Resources.Tag_Day_Button, group: Properties.TagGroups.Day),
        new TagStruct(code: Properties.TagCodes.DayShort, visibleText: Properties.Resources.Tag_DayShort_VisibleText, buttonLabel: Properties.Resources.Tag_DayShort_Button, group: Properties.TagGroups.Day),
        new TagStruct(code: Properties.TagCodes.DayLong, visibleText: Properties.Resources.Tag_DayLong_VisibleText, buttonLabel: Properties.Resources.Tag_DayLong_Button, group: Properties.TagGroups.Day),
        new TagStruct(code: Properties.TagCodes.DeviceName, visibleText: Properties.Resources.Tag_DeviceName_VisibleText, buttonLabel: Properties.Resources.Tag_DeviceName_Button),
        new TagStruct(code: Properties.TagCodes.DeviceManuf, visibleText: Properties.Resources.Tag_DeviceManuf_VisibleText, buttonLabel: Properties.Resources.Tag_DeviceManuf_Button),
        new TagStruct(code: Properties.TagCodes.SequenceNum, visibleText: Properties.Resources.Tag_SequenceNum_VisibleText, buttonLabel: Properties.Resources.Tag_SequenceNum_Button),
        new TagStruct(code: Properties.TagCodes.CustomText, visibleText: Properties.Resources.Tag_CustomText_VisibleText, buttonLabel: Properties.Resources.Tag_CustomText_Button),
        new TagStruct(code: Properties.TagCodes.FileName, visibleText: Properties.Resources.Tag_FileName_VisibleText, buttonLabel: Properties.Resources.Tag_FileName_Button),
        new TagStruct(code: Properties.TagCodes.NewFolder, visibleText: "\\", buttonLabel: Properties.Resources.Tag_NewFolder_Button),
        new TagStruct(code: Properties.TagCodes.Hyphen, visibleText: "-", buttonLabel: "-", group: Properties.TagGroups.Separator),
        new TagStruct(code: Properties.TagCodes.Underscore, visibleText: "_", buttonLabel: "_", group: Properties.TagGroups.Separator)
    };

    private static readonly List<string> dateTags = new()
    {
        Properties.TagCodes.Year,
        Properties.TagCodes.YearLong,
        Properties.TagCodes.Month,
        Properties.TagCodes.MonthShort,
        Properties.TagCodes.MonthLong,
        Properties.TagCodes.Day,
        Properties.TagCodes.DayLong,
        Properties.TagCodes.DayShort
    };

    public static TagStruct GetTag(string code = null, string visibleText = null, string label = null)
    {
        if (code != null)
        {
            if (code.Contains(value: '('))
            {
                code = RemoveParameter(tag: code); //v případě tagu s parametrem {tag}(param) odstraní parametr
            }

            return tagList.First(predicate: x => x.Code == code);
        }
        else if (visibleText != null)
        {
            return tagList.First(predicate: x => x.VisibleText == visibleText);
        }
        else if (label != null)
        {
            return tagList.First(predicate: x => x.ButtonLabel == label);
        }

        return new TagStruct();
    }

    public static bool IsValidFileName(string text)
    {
        return text != string.Empty && text.IndexOfAny(anyOf: Path.GetInvalidFileNameChars()) < 0;
    }

    public static List<TagStruct> GetTagGroup(string code)
    {
        return tagList.Where(predicate: x => x.Group == code).ToList();
    }

    public static string GetParameter(string visibleText)
    {
        return Regex.Match(input: visibleText, pattern: @"(?<=\().+?(?=\))").ToString();
    }

    public static string RemoveParameter(string tag)
    {
        return string.Join(separator: "", values: tag.TakeWhile(predicate: c => c != '('));
    }


    //získání hodnot pro zobrazení náhledu výsledného názvu
    public static string GetSampleValueByTag(string tagCode, MediaItem fileInfo = null, Device device = null, string filePath = null)
    {
        TagStruct tag = GetTag(code: tagCode);
        if (tag != null)
        {
            string codeTag = tag.Code;
            DateTime date = DateTime.Now;
            string manufacturer = GetTag(code: Properties.TagCodes.DeviceManuf).VisibleText;
            string model = GetTag(code: Properties.TagCodes.DeviceName).VisibleText;
            string filename = GetTag(code: Properties.TagCodes.FileName).VisibleText;

            CultureInfo cultureInfo = CultureInfo.CurrentUICulture;
            if (dateTags.Contains(item: codeTag) && Properties.Settings.Default.UseDifferentLangForTags)
            {
                if (Properties.Settings.Default.TagLanguage == nameof(Properties.Languages.system))
                {
                    cultureInfo = CultureInfo.CurrentCulture;
                }
                else
                {
                    cultureInfo = CultureInfo.CreateSpecificCulture(name: Properties.Settings.Default.TagLanguage);
                }
            }

            if (File.Exists(path: filePath) && fileInfo != null)
            {
                if (dateTags.Contains(item: codeTag))
                {
                    date = FileExif.GetDateTimeOriginal(path: filePath);
                    // soubor nemusí obsahovat EXIF informace
                    if (date == new DateTime())
                    {
                        date = fileInfo.CapturedAt?.LocalDateTime ?? default;
                    }
                }

                if (codeTag == Properties.TagCodes.DeviceManuf)
                {
                    manufacturer = FileExif.GetManufacturer(path: filePath);
                    if (manufacturer == null || manufacturer == string.Empty)
                    {
                        manufacturer = device.Manufacturer;
                    }
                }

                if (codeTag == Properties.TagCodes.DeviceName)
                {
                    model = FileExif.GetModel(path: filePath);
                    if (model == null || model == string.Empty)
                    {
                        model = device.Name;
                    }
                }

                if (codeTag == Properties.TagCodes.FileName)
                {
                    filename = Path.GetFileNameWithoutExtension(path: fileInfo.FileName);
                }
            }

            if (codeTag == Properties.TagCodes.YearLong)
            {
                return date.Year.ToString();
            }
            else if (codeTag == Properties.TagCodes.Year)
            {
                return (date.Year % 100).ToString();
            }
            else if (codeTag == Properties.TagCodes.Month)
            {
                return date.Month.ToString(format: "00");
            }
            else if (codeTag == Properties.TagCodes.MonthShort)
            {
                return cultureInfo.DateTimeFormat.GetAbbreviatedMonthName(month: date.Month);
            }
            else if (codeTag == Properties.TagCodes.MonthLong)
            {
                return cultureInfo.DateTimeFormat.GetMonthName(month: date.Month);
            }
            else if (codeTag == Properties.TagCodes.Day)
            {
                return date.Day.ToString(format: "00");
            }
            else if (codeTag == Properties.TagCodes.DayShort)
            {
                return cultureInfo.DateTimeFormat.GetAbbreviatedDayName(dayofweek: date.DayOfWeek);
            }
            else if (codeTag == Properties.TagCodes.DayLong)
            {
                return cultureInfo.DateTimeFormat.GetDayName(dayofweek: date.DayOfWeek);
            }
            else if (codeTag == Properties.TagCodes.DeviceName)
            {
                return model;
            }
            else if (codeTag == Properties.TagCodes.DeviceManuf)
            {
                return manufacturer;
            }
            else if (codeTag == Properties.TagCodes.FileName)
            {
                return filename;
            }
            else if (codeTag == Properties.TagCodes.SequenceNum)
            {
                return "####";
            }
            else if (codeTag == Properties.TagCodes.CustomText)
            {
                return GetParameter(visibleText: tagCode); //vrátí parametr v ()
            }
            else
            {
                return tag.VisibleText;
            }
        }
        else
        {
            return string.Empty;
        }
    }


    //prevede tagy na list
    public static List<string> TagsToList(string tagString)
    {
        var tags = new List<string>();
        while (tagString.Length > 0)
        {
            var tag = Regex.Match(input: tagString, pattern: @"\{.*?\}").ToString(); //ziskani tagu vcetne {}              

            if (tag.Length < tagString.Length && tagString[index: tag.Length].Equals(obj: '('))
            {
                tag += Regex.Match(input: tagString, pattern: @"\(.*?\)").ToString();
            }
            else if (tagString[index: 0] != '{')
            {
                tag = Regex.Match(input: tagString, pattern: @"[^\{]*").ToString(); //získání textu po další {
            }

            tagString = tagString.Remove(startIndex: 0, count: tag.Length);

            tags.Add(item: tag);
        }

        return tags;
    }

    //dosadi hodnoty za tagy
    public static string TagsToValues(List<string> tags, Device device = null, MediaItem fileInfo = null, string filePath = null)
    {
        var values = "";
        foreach (string tag in tags)
        {
            TagStruct tagStruct = GetTag(code: tag);
            if (tagStruct.Group != Properties.TagGroups.Separator)
            {
                if (tag == Properties.TagCodes.CustomText)
                {
                    values += Regex.Match(input: tag, pattern: @"(?<=\().*?(?=\))").ToString(); //ziskani hodnoty cutomText
                }

                values += GetSampleValueByTag(tagCode: tag, fileInfo: fileInfo, device: device, filePath: filePath);
            }
            else
            {
                values += tagStruct.VisibleText;
            }
        }

        return values;
    }

    internal static string TagsToValues(List<List<string>> folderTags, Device device, MediaItem file, string tmpFile)
    {
        var folders = new List<string>();
        folderTags.ForEach(action: x => folders.Add(item: TagsToValues(tags: x, device: device, fileInfo: file, filePath: tmpFile)));
        return string.Join(separator: "\\", values: folders);
    }

    //prevod stringu na strukturu slozek
    public static string TagsToBlock(string folders, string file, Device device = null)
    {
        var tagList = new List<string>();
        var block = "";
        var i = 0;
        while (folders != null && folders.Length > 0)
        {
            if (i == 0)
            {
                block += "\\";
            }

            if (folders[index: 0] == '\\')
            {
                folders = folders.Trim(trimChar: '\\');
                block += "\n" + new string(c: ' ', count: i) + " └> ";
                i++;
            }

            var level = Regex.Match(input: folders, pattern: @"[^\\]*").ToString(); //ziskani urovne bez \
            tagList = TagsToList(tagString: level);
            block += TagsToValues(tags: tagList, device: device);
            folders = folders.Remove(startIndex: 0, count: level.Length);
        }

        if (file != null && file.Length > 0)
        {
            tagList = TagsToList(tagString: file);
            block += "\n" + new string(c: ' ', count: i + 6);
            block += TagsToValues(tags: tagList, device: device) + ".xxx";
        }

        return block;
    }

    public static StackPanel TagsToStackPanel(string folders, string file, Style textStyle, Device device = null)
    {
        var tagList = new List<string>();
        var spMain = new StackPanel();
        var i = 1;
        while (folders != null && folders.Length > 0)
        {
            if (folders[index: 0] == '\\')
            {
                i++;
                folders = folders.Trim(trimChar: '\\');
            }

            var level = Regex.Match(input: folders, pattern: @"[^\\]*").ToString(); //ziskani urovne bez \
            tagList = TagsToList(tagString: level);

            spMain.Children.Add(element: MainWindow.CreateIconPanel(text: TagsToValues(tags: tagList, device: device), iconKind: PackIconKind.FolderOutline, level: i, textStyle: textStyle));

            folders = folders.Remove(startIndex: 0, count: level.Length);
        }

        if (file != null && file.Length > 0)
        {
            tagList = TagsToList(tagString: file);
            spMain.Children.Add(element: MainWindow.CreateIconPanel(text: TagsToValues(tags: tagList, device: device) + ".xxx", iconKind: PackIconKind.ImageOutline, level: i + 1, textStyle: textStyle));
        }

        return spMain;
    }

    public static bool IsValidTag(string text)
    {
        return GetTag(visibleText: text).Code != null;
    }
}
