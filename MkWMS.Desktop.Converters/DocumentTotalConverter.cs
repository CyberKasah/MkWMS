using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using MkWMS.API.DTOs;

namespace MkWMS.Desktop.Converters;

public class DocumentTotalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ObservableCollection<CreateDocumentItemDto> items)
        {
            decimal total = items.Sum(i => i.Quantity * i.Price);
            return total.ToString("N2");
        }
        return "0.00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}