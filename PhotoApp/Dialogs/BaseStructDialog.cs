using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PhotoApp.Properties;
using WirePix.Core.Validation;

namespace PhotoApp.Dialogs;

internal static class BaseStructDialog
{
    public static bool TagAdd(string tagCode, List<string> tags, TextBlock error)
    {
        TagStruct tag = TagPresentation.GetTag(code: tagCode);
        TemplateValidationCode result = NameTemplateValidator.ValidateAppend(tags, tagCode, Settings.Default.MaxTags);
        switch (result)
        {
            case TemplateValidationCode.FirstSeparator:
                error.Text = string.Format(format: Resources.FirstTagError, arg0: TagPresentation.GetTag(code: tagCode).ButtonLabel);
                return false;
            case TemplateValidationCode.TagLimitExceeded:
                error.Text = string.Format(format: Resources.MaxTagError, arg0: Settings.Default.MaxTags);
                return false;
            case TemplateValidationCode.None:
                return true;
        }

        TagStruct lastTag = tags.Count == 0 ? new TagStruct() : TagPresentation.GetTag(code: tags.Last());
        error.Text = string.Format(format: Resources.TagPairError, arg0: tag.ButtonLabel, arg1: lastTag.ButtonLabel);
        return false;
    }

    public static bool ValidCustomText(TextBox textBox)
    {
        return textBox.Visibility != Visibility.Visible ||
               NameTemplateValidator.ValidateCustomText(textBox.Text) == TemplateValidationCode.None;
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