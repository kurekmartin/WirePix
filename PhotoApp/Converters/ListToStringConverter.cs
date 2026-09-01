using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class ListToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var list = (List<string>)value;
        return string.Join(separator: "\n", value: list.ToArray());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}