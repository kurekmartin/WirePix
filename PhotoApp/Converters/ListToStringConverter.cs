using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters;

class ListToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        List<string> list = (List<string>)value;
        return string.Join("\n", list.ToArray());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}