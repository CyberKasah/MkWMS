using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using MkWMS.API.DTOs; // Это важно для доступа к RoleDto

namespace MkWMS.Desktop.Converters
{
    public class RolesToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. Пробуем привести к списку RoleDto (как в вашем UserDto)
            if (value is IEnumerable<RoleDto> roleDtos && roleDtos.Any())
            {
                return string.Join(", ", roleDtos.Select(r => r.Name));
            }

            // 2. На всякий случай оставляем проверку на обычные строки
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