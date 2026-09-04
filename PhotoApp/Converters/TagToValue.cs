using System;
using System.Globalization;
using System.Windows.Data;
using PhotoApp.Dialogs;
using WirePix.Core.Naming.Templates;
using WirePix.Core.Naming.Tokens;

namespace PhotoApp.Converters;

internal class TagToValue : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var tag = value as string;
        string code = NameTemplate.NormalizeCode(code: NameTemplate.RemoveParameter(tag: tag));
        if (code is NamingTokens.DeviceName or NamingTokens.DeviceManufacturer or NamingTokens.FileName)
        {
            return TagPresentation.GetTag(code: code).VisibleText;
        }

        return NameTemplate.Evaluate(tags: [tag], context: new NamingContext());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
