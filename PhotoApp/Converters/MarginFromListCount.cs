using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PhotoApp.Converters;

internal class MarginFromListCount : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var margin = new Thickness(left: 0, top: 0, right: 0, bottom: 0);
        var itemSource = parameter as CollectionViewSource;
        var items = itemSource.Source as ObservableCollection<ObservableCollection<string>>;
        if (items != null)
        {
            margin.Left = items.Count * 10;
        }
        else
        {
            var itemsList = itemSource.Source as List<List<string>>;
            if (itemsList != null)
            {
                margin.Left = itemsList.Count * 10;
            }
        }

        return margin;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}