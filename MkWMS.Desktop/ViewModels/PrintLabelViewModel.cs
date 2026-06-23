using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

/// <summary>
/// ViewModel диалогового окна печати этикетки для товара
/// </summary>
public partial class PrintLabelViewModel : BaseViewModel
{
    private readonly ApiClient _api;
    private readonly int _productId;

    [ObservableProperty]
    private string productName = string.Empty;

    [ObservableProperty]
    private string barcode = string.Empty;

    [ObservableProperty]
    private int quantity = 1;

    public PrintLabelViewModel(ApiClient api, ProductDto product)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _productId = product.Id;

        ProductName = product.Name;
        Barcode = product.Barcode ?? product.Article ?? "—";
        Quantity = 1;
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (Quantity < 1)
        {
            SetError("Количество должно быть больше 0");
            return;
        }

        IsLoading = true;
        ClearError();

        try
        {
            // Формируем URL для печати (предполагаем, что эндпоинт уже есть в ApiClient)
            // Если у тебя в ApiClient есть метод GetPrintFormAsync — лучше использовать его
            var url = $"https://localhost:7000/api/products/label/{_productId}?qty={Quantity}";

            // Вариант 1: Открываем в браузере (как было раньше)
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

            // Вариант 2 (рекомендуемый): если в ApiClient есть метод для получения PDF/печати
            // var pdfBytes = await _api.GetPrintFormAsync(_productId, "label", Quantity);
            // if (pdfBytes != null) { /* сохранить или отправить на принтер */ }

            // Закрываем диалог с успехом
            CloseDialog(true);
        }
        catch (Exception ex)
        {
            SetError($"Ошибка при запуске печати: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(false);
    }

    private void CloseDialog(bool result)
    {
        var window = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext == this);

        if (window != null)
        {
            window.DialogResult = result;
            window.Close();
        }
    }
}