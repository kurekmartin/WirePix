using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PhotoApp.Dialogs;

internal static class BaseStructDialog
{
    public static bool TagAdd(string tagCode, List<string> tags, TextBlock error)
    {
        TagStruct tag = Tags.GetTag(code: tagCode);
        if (tag.Group == Properties.TagGroups.Separator && tags.Count() == 0) //separator na začátku
        {
            error.Text = string.Format(format: Properties.Resources.FirstTagError, arg0: Tags.GetTag(code: tagCode).ButtonLabel);
            return false;
        }
        else if (tags.Count(predicate: x => tag.Group != Properties.TagGroups.Separator) > Properties.Settings.Default.MaxTags) //dosažen max počet tagů
        {
            error.Text = string.Format(format: Properties.Resources.MaxTagError, arg0: Properties.Settings.Default.MaxTags);
            return false;
        }
        else if (tag.Group == Properties.TagGroups.Separator) //dva separatory za sebou
        {
            TagStruct lastTag = Tags.GetTag(code: tags.Last());
            if (lastTag.Group == Properties.TagGroups.Separator)
            {
                error.Text = string.Format(format: Properties.Resources.TagPairError, arg0: tag.ButtonLabel, arg1: lastTag.ButtonLabel);
                return false;
            }
        }

        return true;
    }

    public static bool ValidCustomText(TextBox textBox)
    {
        if (textBox.Visibility != Visibility.Visible)
        {
            return true;
        }
        else
        {
            return Tags.IsValidFileName(text: textBox.Text);
        }
    }

    public static void ShowCustomTextError(TextBox customText, TextBlock errorBlock)
    {
        string text = customText.Text;
        if (text.Length == 0)
        {
            errorBlock.Text = string.Format(format: Properties.Resources.TagCustomTextMissing, arg0: Tags.GetTag(code: Properties.TagCodes.CustomText).VisibleText);
            errorBlock.Visibility = Visibility.Visible;
        }
        else if (text.IndexOfAny(anyOf: Path.GetInvalidFileNameChars()) >= 0)
        {
            List<char> invalidChars = text.Where(predicate: x => Path.GetInvalidFileNameChars().Contains(value: x)).ToList();
            errorBlock.Text = string.Format(format: Properties.Resources.InvalidCharsError, arg0: string.Join(separator: "", values: invalidChars));
            errorBlock.Visibility = Visibility.Visible;
        }

        customText.Focus();
    }
}