using System;
using System.Globalization;
using System.Windows.Data;

namespace PhotoApp.Converters;

class TimeSpanToString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        TimeSpan time = (TimeSpan)value;
        string timeString = "";
        if (time.Ticks == 0 || time.Ticks<0)
        {
            timeString = Properties.Resources.RemainingTime_Calculating;
        }
        else
        {
            timeString = $"{Properties.Resources.RemainingTime_Remains} ";
            if (time.Days > 0)
            {
                timeString += time.Days + $"{Properties.Resources.RemainingTime_Days} ";
            }
            if (time.Hours > 0)
            {
                timeString += time.Hours + $"{Properties.Resources.RemainingTime_Hours} ";
            }
            if (time.Minutes > 0)
            {
                timeString += time.Minutes + $"{Properties.Resources.RemainingTime_Minutes} ";
            }
            timeString += time.Seconds + $"{Properties.Resources.RemainingTime_Seconds} ";
        }
        return timeString;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}