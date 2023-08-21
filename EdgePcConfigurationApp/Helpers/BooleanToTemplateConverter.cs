using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EdgePcConfigurationApp.Helpers;

public class BooleanToTemplateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected && parameter is string templates)
        {
            return isConnected
                ? Application.Current.Resources[templates.Split(';')[0]] as DataTemplate
                : Application.Current.Resources[templates.Split(';')[1]] as DataTemplate;
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}