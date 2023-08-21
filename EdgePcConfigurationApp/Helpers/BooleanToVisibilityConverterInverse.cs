using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EdgePcConfigurationApp.Helpers
{
    class BooleanToVisibilityConverterInverse : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = (bool)value;
            return !boolValue ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

