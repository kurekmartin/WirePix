using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class ListToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is IEnumerable<string> list
            ? string.Join(separator: "\n", values: list)
            : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
