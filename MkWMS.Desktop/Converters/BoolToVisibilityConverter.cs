using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MkWMS.Desktop.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isTrue = false;

        // 1. Логика определения состояния (bool или наличие объекта)
        if (value is bool b)
            isTrue = b;
        else if (value != null)
            isTrue = true;

        // 2. Обработка параметра
        if (parameter is string p)
        {
            // Если в параметре есть '|', значит мы хотим получить текст
            if (p.Contains('|'))
            {
                var parts = p.Split('|');
                var trueText = parts[0];
                var falseText = parts.Length > 1 ? parts[1] : string.Empty;
                return isTrue ? trueText : falseText;
            }

            // Стандартная инверсия для Visibility
            if (p.Equals("inverse", StringComparison.OrdinalIgnoreCase))
            {
                isTrue = !isTrue;
            }
        }

        // 3. Если XAML ожидает строку, но мы не попали в условие с '|'
        if (targetType == typeof(string))
        {
            return isTrue.ToString();
        }

        return isTrue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}