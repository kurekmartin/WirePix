using System;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class IntToDeviceStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if ((int)value == Device.DEVICE_CANNOT_CONNECT)
        {
            return Properties.Resources.DeviceStatus_CannotConnect;
        }
        else if ((int)value == Device.DEVICE_READY)
        {
            return Properties.Resources.DeviceStatus_Ready;
        }
        else
        {
            return Properties.Resources.DeviceStatus_Unknown;
        }
    }

    public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}