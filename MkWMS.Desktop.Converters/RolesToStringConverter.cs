using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using MkWMS.API.DTOs; // Обязательно добавляем ссылку на DTO

namespace MkWMS.Desktop.Converters
{
    public class RolesToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Проверяем на IEnumerable<string>, так как в VM у нас ObservableCollection<string>
            if (value is IEnumerable<string> roles && roles.Any())
            {
                return string.Join(", ", roles);
            }

            return "Нет ролей";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}