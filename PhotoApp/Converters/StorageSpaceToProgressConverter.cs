using System;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class StorageSpaceToProgressConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DeviceStorageSpace { TotalGigabytes: > 0 } space)
        {
            return 100 - space.AvailableGigabytes / space.TotalGigabytes * 100;
        }

        return 100;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
