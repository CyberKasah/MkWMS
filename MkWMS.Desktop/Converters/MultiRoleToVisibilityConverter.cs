using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Collections.Generic;

namespace MkWMS.Desktop.Converters;

public class MultiRoleToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // value должен быть IEnumerable<string> — список ролей пользователя
        if (value is not IEnumerable<string> roles || parameter is not string param)
            return Visibility.Collapsed;

        var requiredRoles = param.Split(',').Select(r => r.Trim());
        return requiredRoles.Any(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase))
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}