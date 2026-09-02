using System;
using System.Globalization;
using System.Windows.Data;
using PhotoApp.Dialogs;
using WirePix.Core.Naming.Templates;

namespace PhotoApp.Converters;

internal class TagCodeToLabel : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s)
        {
            return string.Empty;
        }

        string code;
        string tag = code = s;
        if (!tag.Contains(value: '('))
        {
            return TagPresentation.GetTag(code: code).VisibleText;
        }

        code = NameTemplate.RemoveParameter(tag: tag);
        return $"{TagPresentation.GetTag(code: code).VisibleText}({NameTemplate.GetParameter(tag: tag)})";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}