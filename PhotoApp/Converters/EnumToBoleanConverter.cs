using System;
using System.Windows;
using System.Windows.Data;

namespace PhotoApp.Converters;

public class EnumToBoleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var parameterString = parameter as string;
        if (parameterString == null)
        {
            return DependencyProperty.UnsetValue;
        }

        if (Enum.IsDefined(enumType: value.GetType(), value: value) == false)
        {
            return DependencyProperty.UnsetValue;
        }

        object parameterValue = Enum.Parse(enumType: value.GetType(), value: parameterString);

        return parameterValue.Equals(obj: value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        var parameterString = parameter as string;
        if (parameterString == null)
        {
            return DependencyProperty.UnsetValue;
        }

        return Enum.Parse(enumType: targetType, value: parameterString);
    }
}