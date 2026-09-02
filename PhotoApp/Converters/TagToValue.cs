using System;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class TagToValue : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var tag = value as string;
        return Tags.GetSampleValueByTag(tagCode: tag);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}