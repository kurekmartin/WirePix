using System;
using System.Globalization;
using System.Windows.Data;
using WirePix.Core.Naming.Templates;

namespace PhotoApp.Converters;

internal class TagToValue : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var tag = value as string;
        return NameTemplate.Evaluate(tags: [tag], context: new NamingContext());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
