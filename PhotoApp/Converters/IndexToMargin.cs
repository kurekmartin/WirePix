using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class IndexToMargin : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int)
        {
            int margin = (int)value * 10;
            return new Thickness(left: margin, top: 0, right: 0, bottom: 0);
        }

        return new Thickness(uniformLength: 0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}