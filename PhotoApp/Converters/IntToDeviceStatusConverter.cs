using System;
using System.Windows.Data;
using WirePix.Devices.Enums;

namespace PhotoApp.Converters;

internal class IntToDeviceStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value switch
        {
            MediaDeviceState state when state != MediaDeviceState.Ready => Properties.Resources.DeviceStatus_CannotConnect,
            MediaDeviceState.Ready => Properties.Resources.DeviceStatus_Ready,
            _ => Properties.Resources.DeviceStatus_Unknown
        };
    }

    public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
