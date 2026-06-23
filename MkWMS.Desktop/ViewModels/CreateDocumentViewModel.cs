using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class CreateDocumentViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private CreateDocumentDto _document = new();
    [ObservableProperty] private ObservableCollection<CreateDocumentItemDto> _items = new();

    // Справочники
    [ObservableProperty] private ObservableCollection<DocumentTypeDto> _documentTypes = new();
    [ObservableProperty] private ObservableCollection<WarehouseDto> _warehouses = new();
    [ObservableProperty] private ObservableCollection<ProductDto> _products = new();
    [ObservableProperty] private ObservableCollection<CounterpartyDto> _counterparties = new();

    public CreateDocumentViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        Document.ExternalDate = DateTime.Now;

        // Подписываемся на изменения в списке товаров, чтобы обновлять Итого
        Items.CollectionChanged += Items_CollectionChanged;

        _ = LoadDictionariesAsync();
    }

    private async Task LoadDictionariesAsync()
    {
        IsLoading = true;
        ClearError();
        try
        {
            var types = await _apiClient.GetDocumentTypesAsync();
            if (types != null) DocumentTypes = new(types);

            var req = new PagedRequestDto { Page = 1, PageSize = 1000 };

            var warehousesResult = await _apiClient.GetWarehousesAsync(req);
            if (warehousesResult?.Items != null) Warehouses = new(warehousesResult.Items);

            var productsResult = await _apiClient.GetProductsAsync(req);
            if (productsResult?.Items != null) Products = new(productsResult.Items);

            var counterpartiesResult = await _apiClient.GetCounterpartiesAsync(req);
            if (counterpartiesResult?.Items != null) Counterparties = new(counterpartiesResult.Items);
        }
        catch (Exception ex) { SetError($"Ошибка справочников: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void AddItem()
    {
        var newItem = new CreateDocumentItemDto { Quantity = 1, Price = 0 };
        // Подписываемся на изменения свойств внутри DTO (если это поддерживается)
        // Или просто добавляем в коллекцию
        Items.Add(newItem);
    }

    [RelayCommand]
    private void RemoveItem(CreateDocumentItemDto? item)
    {
        if (item != null) Items.Remove(item);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsLoading) return;

        Document.Items = Items.ToList();

        // Валидация
        if (Document.DocumentTypeId <= 0) { SetError("Выберите тип документа"); return; }
        if (Document.WarehouseId <= 0) { SetError("Выберите склад"); return; }
        if (!Document.Items.Any()) { SetError("Добавьте позиции"); return; }
        if (Document.Items.Any(i => i.ProductId <= 0)) { SetError("Укажите товар во всех строках"); return; }

        IsLoading = true;
        ClearError();

        try
        {
            // !!! ВАЖНО: Мы не ставим "NEW". Номер сгенерирует сервер по SEQUENCE.
            // Если серверу нужен пустой номер для генерации - оставляем null.
            Document.Number = null;

            var newId = await _apiClient.CreateDocumentAsync(Document);
            if (newId > 0)
            {
                CloseWindow(true);
            }
            else
            {
                SetError("Ошибка сохранения на сервере");
            }
        }
        catch (Exception ex)
        {
            SetError($"Ошибка при сохранении: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void Cancel() => CloseWindow(false);

    private void CloseWindow(bool result)
    {
        var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
        if (window != null)
        {
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal) window.DialogResult = result;
            window.Close();
        }
    }

    // Логика обновления общих сумм при изменении состава строк
    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Уведомляем интерфейс, что общая сумма документа могла измениться
        OnPropertyChanged(nameof(TotalDocumentSum));
    }

    public decimal TotalDocumentSum => Items.Sum(i => i.TotalSum);
}