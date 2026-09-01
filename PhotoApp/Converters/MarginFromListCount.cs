using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PhotoApp.Converters;

class MarginFromListCount : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Thickness margin = new Thickness(0, 0, 0, 0);
        CollectionViewSource itemSource = parameter as CollectionViewSource;
        var items = itemSource.Source as ObservableCollection<ObservableCollection<string>>;
        if (items != null)
        {
            margin.Left = items.Count * 10;
        }
        else
        {
            var itemsList = itemSource.Source as List<List<string>>;
            if(itemsList != null)
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