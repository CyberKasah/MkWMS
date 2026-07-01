using System;
using System.Globalization;
using System.Windows.Data;
using MkWMS.API.DTOs;

namespace MkWMS.Desktop.Converters;

public class ItemSumConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        decimal quantity = 0;
        decimal price = 0;
        decimal vatSum = 0;


        if (value is CreateDocumentItemDto cItem)
        {
            quantity = cItem.Quantity;
            price = cItem.Price;
            vatSum = cItem.VatSum;
        }
        else if (value is DocumentItemDto dItem)
        {
            quantity = dItem.Quantity;
            price = dItem.Price ?? 0;
            vatSum = dItem.VatSum;
        }
        else return "0.00";


        if (parameter?.ToString() == "WithVat")
        {
            return (quantity * price + vatSum).ToString("N2");
        }


        return (quantity * price).ToString("N2");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}