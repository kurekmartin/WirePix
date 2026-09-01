using System;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class IntToFileSearchStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if ((int)value == Device.DEVICE_FILES_SEARCHING)
        {
            return Properties.Resources.FileSearchStatus_Searching;
        }
        else if ((int)value == Device.DEVICE_FILES_READY)
        {
            return Properties.Resources.FileSearchStatus_Ready;
        }
        else
        {
            return Properties.Resources.FileSearchStatus_Unknown;
        }
    }

    public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}