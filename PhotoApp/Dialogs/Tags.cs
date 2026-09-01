using MaterialDesignThemes.Wpf;
using MediaDevices;
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
        return (n1.Code == n2.Code && n1.VisibleText == n2.VisibleText && n1.ButtonLabel == n2.ButtonLabel);
    }
    public static bool operator !=(TagStruct n1, TagStruct n2)
    {
        return (n1.Code != n2.Code || n1.VisibleText != n2.VisibleText || n1.ButtonLabel != n2.ButtonLabel);
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
            buttons.Add(new ButtonStruct(btn));
        }

        gridPosition = gridPos;
    }
    public string groupName { get; }
    public List<ButtonStruct> buttons { get; }
    public Point gridPosition { get; }
}
static class Tags
{
    private static readonly List<TagStruct> tagList = new List<TagStruct>()
    {
        //            code                            visible text                                         button label                                         groups
        new TagStruct(Properties.TagCodes.YearLong,   Properties.Resources.Tag_YearLong_VisibleText,       Properties.Resources.Tag_YearLong_Button,            Properties.TagGroups.Year),
        new TagStruct(Properties.TagCodes.Year,       Properties.Resources.Tag_YearShort_VisibleText,      Properties.Resources.Tag_YearShort_Button,           Properties.TagGroups.Year),
        new TagStruct(Properties.TagCodes.Month,      Properties.Resources.Tag_Month_VisibleText,          Properties.Resources.Tag_Month_Button,               Properties.TagGroups.Month),
        new TagStruct(Properties.TagCodes.MonthShort, Properties.Resources.Tag_MonthShort_VisibleText,     Properties.Resources.Tag_MonthShort_Button,          Properties.TagGroups.Month),
        new TagStruct(Properties.TagCodes.MonthLong,  Properties.Resources.Tag_MonthLong_VisibleText,      Properties.Resources.Tag_MonthLong_Button,           Properties.TagGroups.Month),
        new TagStruct(Properties.TagCodes.Day,        Properties.Resources.Tag_Day_VisibleText,            Properties.Resources.Tag_Day_Button,                 Properties.TagGroups.Day),
        new TagStruct(Properties.TagCodes.DayShort,   Properties.Resources.Tag_DayShort_VisibleText,       Properties.Resources.Tag_DayShort_Button,            Properties.TagGroups.Day),
        new TagStruct(Properties.TagCodes.DayLong,    Properties.Resources.Tag_DayLong_VisibleText,        Properties.Resources.Tag_DayLong_Button,             Properties.TagGroups.Day),
        new TagStruct(Properties.TagCodes.DeviceName, Properties.Resources.Tag_DeviceName_VisibleText,     Properties.Resources.Tag_DeviceName_Button),
        new TagStruct(Properties.TagCodes.DeviceManuf,Properties.Resources.Tag_DeviceManuf_VisibleText,    Properties.Resources.Tag_DeviceManuf_Button),
        new TagStruct(Properties.TagCodes.SequenceNum,Properties.Resources.Tag_SequenceNum_VisibleText,    Properties.Resources.Tag_SequenceNum_Button),
        new TagStruct(Properties.TagCodes.CustomText, Properties.Resources.Tag_CustomText_VisibleText,     Properties.Resources.Tag_CustomText_Button),
        new TagStruct(Properties.TagCodes.FileName,   Properties.Resources.Tag_FileName_VisibleText,       Properties.Resources.Tag_FileName_Button),
        new TagStruct(Properties.TagCodes.NewFolder,  "\\",                                                Properties.Resources.Tag_NewFolder_Button),
        new TagStruct(Properties.TagCodes.Hyphen,     "-",                                                 "-",                                                 Properties.TagGroups.Separator),
        new TagStruct(Properties.TagCodes.Underscore, "_",                                                 "_",                                                 Properties.TagGroups.Separator)
    };
    private static readonly List<string> dateTags = new List<string>(){
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
            if (code.Contains('('))
            {
                code = RemoveParameter(code); //v případě tagu s parametrem {tag}(param) odstraní parametr
            }
            return tagList.First(x => x.Code == code);
        }
        else if (visibleText != null)
        { 
            return tagList.First(x => x.VisibleText == visibleText);
        }
        else if (label != null)
        {
            return tagList.First(x => x.ButtonLabel == label);
        }
        return new TagStruct();
    }

    public static bool IsValidFileName(string text)
    {
        return text != string.Empty && text.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
    }

    public static List<TagStruct> GetTagGroup(string code)
    {
        return tagList.Where(x => x.Group == code).ToList();
    }

    public static string GetParameter(string visibleText)
    {
        return Regex.Match(visibleText, @"(?<=\().+?(?=\))").ToString();
    }

    public static string RemoveParameter(string tag)
    {
        return String.Join("",tag.TakeWhile(c => c != '('));
    }


    //získání hodnot pro zobrazení náhledu výsledného názvu
    public static string GetSampleValueByTag(string tagCode, MediaFileInfo fileInfo = null, Device device = null, string filePath = null)
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
            if (dateTags.Contains(codeTag) && Properties.Settings.Default.UseDifferentLangForTags)
            {
                if(Properties.Settings.Default.TagLanguage == nameof(Properties.Languages.system))
                {
                    cultureInfo = CultureInfo.CurrentCulture;
                }
                else
                {
                    cultureInfo = CultureInfo.CreateSpecificCulture(Properties.Settings.Default.TagLanguage);
                }
            }

            if (File.Exists(filePath) && fileInfo != null)
            {
                if (dateTags.Contains(codeTag))
                {
                    date = FileExif.GetDateTimeOriginal(filePath);
                    // soubor nemusí obsahovat EXIF informace
                    if (date == new DateTime())
                    {
                        date = (DateTime)fileInfo.CreationTime; // nemusí obsahovat správné datum (někdy se používá DateAuthored nebo LastWriteTime)
                        if (date == new DateTime())
                        {
                            date = (DateTime)fileInfo.DateAuthored;
                        }
                        if (date == new DateTime())
                        {
                            date = (DateTime)fileInfo.LastWriteTime;
                        }
                    }

                }

                if (codeTag == Properties.TagCodes.DeviceManuf)
                {
                    manufacturer = FileExif.GetManufacturer(filePath);
                    if (manufacturer == null || manufacturer == string.Empty)
                    {
                        manufacturer = device.Manufacturer;
                    }
                }

                if (codeTag == Properties.TagCodes.DeviceName)
                {
                    model = FileExif.GetModel(filePath);
                    if (model == null || model == string.Empty)
                    {
                        model = device.Name;
                    }
                }

                if (codeTag == Properties.TagCodes.FileName)
                {
                    filename = Path.GetFileNameWithoutExtension(fileInfo.Name);
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
                return date.Month.ToString("00");
            }
            else if (codeTag == Properties.TagCodes.MonthShort)
            {
                return cultureInfo.DateTimeFormat.GetAbbreviatedMonthName(date.Month);
            }
            else if (codeTag == Properties.TagCodes.MonthLong)
            {
                return cultureInfo.DateTimeFormat.GetMonthName(date.Month);
            }
            else if (codeTag == Properties.TagCodes.Day)
            {
                return date.Day.ToString("00");
            }
            else if (codeTag == Properties.TagCodes.DayShort)
            {
                return cultureInfo.DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek);
            }
            else if (codeTag == Properties.TagCodes.DayLong)
            {
                return cultureInfo.DateTimeFormat.GetDayName(date.DayOfWeek);
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
                return GetParameter(tagCode); //vrátí parametr v ()
            }
            else { return tag.VisibleText; }
        }
        else { return string.Empty; }
    }



    //prevede tagy na list
    public static List<string> TagsToList(string tagString)
    {
        List<string> tags = new List<string>();
        while (tagString.Length > 0)
        {
            string tag = Regex.Match(tagString, @"\{.*?\}").ToString(); //ziskani tagu vcetne {}              

            if (tag.Length < tagString.Length && tagString[tag.Length].Equals('('))
            {
                tag += Regex.Match(tagString, @"\(.*?\)").ToString();
            }
            else if (tagString[0] != '{')
            {
                tag = Regex.Match(tagString, @"[^\{]*").ToString(); //získání textu po další {
            }

            tagString = tagString.Remove(0, tag.Length);

            tags.Add(tag);
        }
        return tags;
    }

    //dosadi hodnoty za tagy
    public static string TagsToValues(List<string> tags, Device device = null, MediaFileInfo fileInfo = null, string filePath = null)
    {
        string values = "";
        foreach (string tag in tags)
        {
            TagStruct tagStruct = GetTag(code: tag);
            if (tagStruct.Group != Properties.TagGroups.Separator)
            {
                if (tag == Properties.TagCodes.CustomText)
                {
                    values += Regex.Match(tag, @"(?<=\().*?(?=\))").ToString(); //ziskani hodnoty cutomText
                }
                values += GetSampleValueByTag(tag, fileInfo, device, filePath);
            }
            else
            {
                values += tagStruct.VisibleText;
            }
        }
        return values;
    }
    internal static string TagsToValues(List<List<string>> folderTags, Device device, MediaFileInfo file, string tmpFile)
    {
        List<string> folders = new List<string>();
        folderTags.ForEach(x => folders.Add(TagsToValues(x, device, file, tmpFile)));
        return string.Join("\\", folders);
    }

    //prevod stringu na strukturu slozek
    public static string TagsToBlock(string folders, string file, Device device = null)
    {
        List<string> tagList = new List<string>();
        string block = "";
        int i = 0;
        while (folders != null && folders.Length > 0)
        {
            if (i == 0) { block += "\\"; }
            if (folders[0] == '\\')
            {
                folders = folders.Trim('\\');
                block += "\n" + new string(' ', i) + " └> ";
                i++;
            }
            string level = Regex.Match(folders, @"[^\\]*").ToString(); //ziskani urovne bez \
            tagList = TagsToList(level);
            block += TagsToValues(tagList, device);
            folders = folders.Remove(0, level.Length);
        }

        if (file != null && file.Length > 0)
        {
            tagList = TagsToList(file);
            block += "\n" + new string(' ', i + 6);
            block += TagsToValues(tagList, device) + ".xxx";
        }

        return block;
    }
    public static StackPanel TagsToStackPanel(string folders, string file, Style textStyle, Device device = null)
    {
        List<string> tagList = new List<string>();
        StackPanel spMain = new StackPanel();
        int i = 1;
        while (folders != null && folders.Length > 0)
        {
            if (folders[0] == '\\')
            {
                i++;
                folders = folders.Trim('\\');
            }
            string level = Regex.Match(folders, @"[^\\]*").ToString(); //ziskani urovne bez \
            tagList = TagsToList(level);

            spMain.Children.Add(MainWindow.CreateIconPanel(TagsToValues(tagList, device), PackIconKind.FolderOutline, i, textStyle));

            folders = folders.Remove(0, level.Length);
        }

        if (file != null && file.Length > 0)
        {
            tagList = TagsToList(file);
            spMain.Children.Add(MainWindow.CreateIconPanel(TagsToValues(tagList, device) + ".xxx", PackIconKind.ImageOutline, i + 1, textStyle));
        }

        return spMain;
    }

    public static bool IsValidTag(string text)
    {
        return GetTag(visibleText: text).Code != null;
    }


}