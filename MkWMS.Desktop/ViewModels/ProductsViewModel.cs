using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.Views.Dialogs;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class ProductsViewModel : BaseCrudViewModel<ProductDto>
{
    public BatchesViewModel BatchesVM { get; }
    public SerialNumbersViewModel SerialNumbersVM { get; }

    public ProductsViewModel(
        ApiClient api,
        BatchesViewModel batchesVM,
        SerialNumbersViewModel serialNumbersVM)
        : base(api, ApiEndpoints.Products)
    {
        BatchesVM = batchesVM;
        SerialNumbersVM = serialNumbersVM;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await LoadAsync();
        await Task.Delay(100);
        await BatchesVM.LoadAsync();
        await Task.Delay(100);
        await SerialNumbersVM.LoadAsync();
    }

    protected override void OnEditSelected(ProductDto item)
    {

        AppMessageBoxWindow.Show($"Редактирование товара: {item.Name}", "Информация", AppMessageBoxIcon.Info);
    }
    public bool CanEditProduct => SelectedItem != null;

    [RelayCommand]
    public void CreateNew()
    {
        ClearError();
        SelectedItem = new ProductDto
        {
            Id = 0,
            UseSerialNumbers = false,
            UseBatches = false,
            CreatedDate = DateTime.Now,
            VatRate = 22m,
            PurchasePrice = 0m,
            RetailPrice = 0m,
            IsMarked = false,
            IsVet = false
        };
    }



    public override async Task DeleteAsync()
    {
        if (SelectedItem == null || SelectedItem.Id <= 0) return;

        var confirmed = AppMessageBoxWindow.Confirm(
            $"Удалить товар «{SelectedItem.Name}» и все связанные партии/серийники?",
            "Подтверждение удаления");

        if (!confirmed) return;

        IsLoading = true;
        ClearError();

        try
        {
            var success = await _api.DeleteAsync(_endpoint, SelectedItem.Id);
            if (success)
            {
                SelectedItem = null;
                await LoadAsync();
                AppMessageBoxWindow.Show("Товар успешно удалён", "Готово", AppMessageBoxIcon.Success);
            }
            else
            {

                var reason = _api.LastErrorMessage ?? "Не удалось удалить. Товар используется в документах.";
                SetError(reason);
                AppMessageBoxWindow.Show(reason, "Не удалось удалить", AppMessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SetError($"Ошибка удаления: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(IsEntitySaved))]
    private void PrintLabel()
    {
        if (SelectedItem == null) return;
        var printVm = new PrintLabelViewModel(_api, SelectedItem);
        var dialog = new PrintLabelDialog(printVm);
        dialog.ShowDialog();
    }

    private bool IsEntitySaved => SelectedItem?.Id > 0;

    protected override async void OnRfidScanned(string rfid)
    {
        var result = await _api.GetItemByRfidAsync(rfid);
        if (result?.Type == "Product")
        {
            AppMessageBoxWindow.Show($"Найден товар ID: {result.ProductId}", "RFID-скан", AppMessageBoxIcon.Info);
        }
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(SelectedItem))
        {
            EditSelectedCommand.NotifyCanExecuteChanged();
            DeleteCommand.NotifyCanExecuteChanged();
            PrintLabelCommand.NotifyCanExecuteChanged();
        }
    }
}