using System;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters;

class TagToValue : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string tag = value as string;
        return Tags.GetSampleValueByTag(tag);

    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}