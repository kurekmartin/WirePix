using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class TagCodeToLabel : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string code, tag;
        if (value is string)
        {
            tag = code = value as string;
            if (tag.Contains(value: "("))
            {
                code = Tags.RemoveParameter(tag: tag);
                return $"{Tags.GetTag(code: code).VisibleText}({Tags.GetParameter(visibleText: tag)})";
            }
            else
            {
                return Tags.GetTag(code: code).VisibleText;
            }
        }
        else
        {
            return string.Empty;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}