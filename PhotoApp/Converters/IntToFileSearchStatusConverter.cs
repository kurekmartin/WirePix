using System;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class IntToFileSearchStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value switch
        {
            FileSearchState.Searching => Properties.Resources.FileSearchStatus_Searching,
            FileSearchState.Ready => Properties.Resources.FileSearchStatus_Ready,
            _ => Properties.Resources.FileSearchStatus_Unknown
        };
    }

    public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
