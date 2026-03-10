using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Collections.Generic;

namespace MkWMS.Desktop.Converters;

public class RoleToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<string> roles || parameter is not string requiredRole)
            return Visibility.Collapsed;

        var hasRole = roles.Any(r =>
            string.Equals(r.Trim(), requiredRole.Trim(), StringComparison.OrdinalIgnoreCase));

        return hasRole ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}