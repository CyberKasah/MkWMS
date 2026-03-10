using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class PrintLabelViewModel : ObservableObject
{
    [ObservableProperty]
    private string productName = string.Empty;

    [ObservableProperty]
    private string barcode = string.Empty;

    [ObservableProperty]
    private int quantity = 1;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    private readonly int _productId;

    public PrintLabelViewModel(ProductDto product)
    {
        _productId = product.Id;
        ProductName = product.Name;
        Barcode = product.Barcode ?? product.Article ?? "—";
        Quantity = 1;
    }

    [RelayCommand]
    private void Print()
    {
        if (Quantity < 1)
        {
            ErrorMessage = "Количество должно быть больше 0";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var url = $"https://localhost:7000/api/products/label/{_productId}?qty={Quantity}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

            var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Ошибка печати: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
        if (window != null)
        {
            window.DialogResult = false;
            window.Close();
        }
    }
}