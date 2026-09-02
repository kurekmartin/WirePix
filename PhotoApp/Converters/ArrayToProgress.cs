using System;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class ArrayToProgress : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var array = value as double[];
        if (array.Length >= 2 && array[0] > 0)
        {
            return 100 - array[1] / array[0] * 100;
        }

        return 100;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}