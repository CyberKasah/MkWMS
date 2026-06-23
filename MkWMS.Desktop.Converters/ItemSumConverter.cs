using System;
using System.Globalization;
using System.Windows.Data;
using MkWMS.API.DTOs;

namespace MkWMS.Desktop.Converters;

public class ItemSumConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is CreateDocumentItemDto item)
        {
            decimal sum = item.Quantity * item.Price;
            return sum.ToString("N2");
        }
        return "0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}