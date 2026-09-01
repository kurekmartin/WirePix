using System;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class LastBackupConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var date = (DateTime)value;
        if (date == new DateTime())
        {
            return Properties.Resources.LastBackup_Never;
        }
        else
        {
            return date.ToString(format: "dd.MM.yyyy HH:mm:ss");
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}