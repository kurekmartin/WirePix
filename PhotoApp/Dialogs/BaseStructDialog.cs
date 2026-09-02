using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PhotoApp.Properties;
using WirePix.Core.Naming.Templates;

namespace PhotoApp.Dialogs;

internal static class BaseStructDialog
{
    public static bool TagAdd(string tagCode, List<string> tags, TextBlock error)
    {
        TagStruct tag = TagPresentation.GetTag(code: tagCode);
        if (tag.Group == TagGroups.Separator && tags.Count == 0) //separator na začátku
        {
            error.Text = string.Format(format: Resources.FirstTagError, arg0: TagPresentation.GetTag(code: tagCode).ButtonLabel);
            return false;
        }

        if (tags.Count(predicate: x => tag.Group != TagGroups.Separator) > Settings.Default.MaxTags) //dosažen max počet tagů
        {
            error.Text = string.Format(format: Resources.MaxTagError, arg0: Settings.Default.MaxTags);
            return false;
        }

        if (tag.Group != TagGroups.Separator) //dva separatory za sebou
        {
            return true;
        }

        TagStruct lastTag = TagPresentation.GetTag(code: tags.Last());
        if (lastTag.Group != TagGroups.Separator)
        {
            return true;
        }

        error.Text = string.Format(format: Resources.TagPairError, arg0: tag.ButtonLabel, arg1: lastTag.ButtonLabel);
        return false;

    }

    public static bool ValidCustomText(TextBox textBox)
    {
        return textBox.Visibility != Visibility.Visible || NameTemplate.IsValidFileName(text: textBox.Text);
    }

    public static void ShowCustomTextError(TextBox customText, TextBlock errorBlock)
    {
        string text = customText.Text;
        if (text.Length == 0)
        {
            errorBlock.Text = string.Format(format: Resources.TagCustomTextMissing, arg0: TagPresentation.GetTag(code: TagCodes.CustomText).VisibleText);
            errorBlock.Visibility = Visibility.Visible;
        }
        else if (text.IndexOfAny(anyOf: Path.GetInvalidFileNameChars()) >= 0)
        {
            List<char> invalidChars = text.Where(predicate: x => Path.GetInvalidFileNameChars().Contains(value: x)).ToList();
            errorBlock.Text = string.Format(format: Resources.InvalidCharsError, arg0: string.Join(separator: "", values: invalidChars));
            errorBlock.Visibility = Visibility.Visible;
        }

        customText.Focus();
    }
}