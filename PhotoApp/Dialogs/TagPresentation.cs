using System.Collections.Generic;
using System.Linq;
using System;
using System.Windows;
using WirePix.Core.Naming.Tokens;

namespace PhotoApp.Dialogs;

public readonly struct TagStruct(
    string code,
    string visibleText,
    string buttonLabel,
    string group = "")
{
    public string Code { get; } = code;
    public string VisibleText { get; } = visibleText;
    public string ButtonLabel { get; } = buttonLabel;
    public string Group { get; } = group;

    public static bool operator ==(TagStruct left, TagStruct right) =>
        left.Code == right.Code && left.VisibleText == right.VisibleText && left.ButtonLabel == right.ButtonLabel;

    public static bool operator !=(TagStruct left, TagStruct right) => !(left == right);

    public override bool Equals(object obj) => obj is TagStruct other && this == other;

    public override int GetHashCode() => HashCode.Combine(Code, VisibleText, ButtonLabel);
}

public readonly struct ButtonStruct
{
    public ButtonStruct(string code)
    {
        TagStruct tag = TagPresentation.GetTag(code: code);
        btnText = tag.ButtonLabel;
        insertValue = tag.Code;
    }

    public string btnText { get; }
    public string insertValue { get; }
}

public readonly struct ButtonGroupStruct
{
    public ButtonGroupStruct(string groupName, List<string> btns, Point gridPos)
    {
        this.groupName = groupName;
        buttons = btns.Select(selector: code => new ButtonStruct(code: code)).ToList();
        gridPosition = gridPos;
    }

    public string groupName { get; }
    public List<ButtonStruct> buttons { get; }
    public Point gridPosition { get; }
}

public static class TagPresentation
{
    public static TagStruct GetTag(string code)
    {
        TagDefinition definition = TagCatalog.Get(code: code);
        if (definition == null)
        {
            return new TagStruct();
        }

        return new TagStruct(
            code: definition.Code,
            visibleText: GetVisibleText(code: definition.Code),
            buttonLabel: GetButtonLabel(code: definition.Code),
            group: definition.Group);
    }

    public static List<TagStruct> GetTagGroup(string group)
    {
        return TagCatalog.GetGroup(group: group).Select(selector: definition => GetTag(code: definition.Code)).ToList();
    }

    private static string GetVisibleText(string code)
    {
        return code switch
        {
            NamingTokens.YearLong => Properties.Resources.Tag_YearLong_VisibleText,
            NamingTokens.Year => Properties.Resources.Tag_YearShort_VisibleText,
            NamingTokens.Month => Properties.Resources.Tag_Month_VisibleText,
            NamingTokens.MonthShort => Properties.Resources.Tag_MonthShort_VisibleText,
            NamingTokens.MonthLong => Properties.Resources.Tag_MonthLong_VisibleText,
            NamingTokens.Day => Properties.Resources.Tag_Day_VisibleText,
            NamingTokens.DayShort => Properties.Resources.Tag_DayShort_VisibleText,
            NamingTokens.DayLong => Properties.Resources.Tag_DayLong_VisibleText,
            NamingTokens.DeviceName => Properties.Resources.Tag_DeviceName_VisibleText,
            NamingTokens.DeviceManufacturer => Properties.Resources.Tag_DeviceManuf_VisibleText,
            NamingTokens.Sequence => Properties.Resources.Tag_SequenceNum_VisibleText,
            NamingTokens.CustomText => Properties.Resources.Tag_CustomText_VisibleText,
            NamingTokens.FileName => Properties.Resources.Tag_FileName_VisibleText,
            NamingTokens.NewFolder => "\\",
            NamingTokens.Hyphen => "-",
            NamingTokens.Underscore => "_",
            _ => string.Empty
        };
    }

    private static string GetButtonLabel(string code)
    {
        return code switch
        {
            NamingTokens.YearLong => Properties.Resources.Tag_YearLong_Button,
            NamingTokens.Year => Properties.Resources.Tag_YearShort_Button,
            NamingTokens.Month => Properties.Resources.Tag_Month_Button,
            NamingTokens.MonthShort => Properties.Resources.Tag_MonthShort_Button,
            NamingTokens.MonthLong => Properties.Resources.Tag_MonthLong_Button,
            NamingTokens.Day => Properties.Resources.Tag_Day_Button,
            NamingTokens.DayShort => Properties.Resources.Tag_DayShort_Button,
            NamingTokens.DayLong => Properties.Resources.Tag_DayLong_Button,
            NamingTokens.DeviceName => Properties.Resources.Tag_DeviceName_Button,
            NamingTokens.DeviceManufacturer => Properties.Resources.Tag_DeviceManuf_Button,
            NamingTokens.Sequence => Properties.Resources.Tag_SequenceNum_Button,
            NamingTokens.CustomText => Properties.Resources.Tag_CustomText_Button,
            NamingTokens.FileName => Properties.Resources.Tag_FileName_Button,
            NamingTokens.NewFolder => Properties.Resources.Tag_NewFolder_Button,
            NamingTokens.Hyphen => "-",
            NamingTokens.Underscore => "_",
            _ => string.Empty
        };
    }
}