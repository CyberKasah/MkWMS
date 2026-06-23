using System;
using System.Globalization;
using System.Windows.Data;

namespace MkWMS.Desktop.Converters;

public class ShowHideConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isVisible)
        {
            // По умолчанию возвращаем "показать" (глаз) когда пароль скрыт
            return isVisible ? "🙈" : "👁️";
        }
        return "👁️";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}