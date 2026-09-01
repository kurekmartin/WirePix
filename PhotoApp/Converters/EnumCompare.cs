using System;
using System.Windows.Data;

namespace PhotoApp;

public class EnumCompare : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value?.Equals(obj: parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value?.Equals(obj: true) == true ? parameter : Binding.DoNothing;
    }
}