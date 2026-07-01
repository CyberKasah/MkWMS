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


    [ObservableProperty] private ObservableCollection<DocumentTypeDto> _documentTypes = new();
    [ObservableProperty] private ObservableCollection<WarehouseDto> _warehouses = new();
    [ObservableProperty] private ObservableCollection<ProductDto> _products = new();
    [ObservableProperty] private ObservableCollection<CounterpartyDto> _counterparties = new();



    [ObservableProperty] private ObservableCollection<StorageLocationDto> _storageLocations = new();
    [ObservableProperty] private ObservableCollection<StorageLocationDto> _filteredStorageLocations = new();

    public CreateDocumentViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        Document.ExternalDate = DateTime.Now;


        Items.CollectionChanged += Items_CollectionChanged;


        Document.PropertyChanged += Document_PropertyChanged;

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

            var locationsResult = await _apiClient.GetStorageLocationsAsync(req);
            if (locationsResult?.Items != null) StorageLocations = new(locationsResult.Items);
            UpdateFilteredLocations();
        }
        catch (Exception ex) { SetError($"Ошибка справочников: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    private void Document_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CreateDocumentDto.WarehouseId))
            UpdateFilteredLocations();
    }

    private void UpdateFilteredLocations()
    {
        FilteredStorageLocations = new ObservableCollection<StorageLocationDto>(
            StorageLocations.Where(l => l.WarehouseId == Document.WarehouseId));
    }

    [RelayCommand]
    private void AddItem()
    {
        var newItem = new CreateDocumentItemDto { Quantity = 1, Price = 0 };


        SubscribeItem(newItem);
        Items.Add(newItem);
    }

    [RelayCommand]
    private void RemoveItem(CreateDocumentItemDto? item)
    {
        if (item != null)
        {
            item.PropertyChanged -= Item_PropertyChanged;
            Items.Remove(item);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsLoading) return;

        Document.Items = Items.ToList();


        if (Document.DocumentTypeId <= 0) { SetError("Выберите тип документа"); return; }
        if (Document.WarehouseId <= 0) { SetError("Выберите склад"); return; }
        if (!Document.Items.Any()) { SetError("Добавьте позиции"); return; }
        if (Document.Items.Any(i => i.ProductId <= 0)) { SetError("Укажите товар во всех строках"); return; }

        IsLoading = true;
        ClearError();

        try
        {


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


    private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {

        OnPropertyChanged(nameof(TotalDocumentSum));



        if (e.NewItems != null)
            foreach (var obj in e.NewItems)
                if (obj is CreateDocumentItemDto item)
                    SubscribeItem(item);
    }

    public decimal TotalDocumentSum => Items.Sum(i => i.TotalSum);




    private void SubscribeItem(CreateDocumentItemDto item)
    {
        item.PropertyChanged -= Item_PropertyChanged;
        item.PropertyChanged += Item_PropertyChanged;
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not CreateDocumentItemDto item) return;



        if (e.PropertyName == nameof(CreateDocumentItemDto.ProductId) ||
            e.PropertyName == nameof(CreateDocumentItemDto.Quantity) ||
            e.PropertyName == nameof(CreateDocumentItemDto.Price))
        {
            RecalculateItem(item, e.PropertyName == nameof(CreateDocumentItemDto.ProductId));
        }

        OnPropertyChanged(nameof(TotalDocumentSum));
    }

    private void RecalculateItem(CreateDocumentItemDto item, bool productJustChanged)
    {
        var product = Products.FirstOrDefault(p => p.Id == item.ProductId);
        if (product == null) return;


        if (productJustChanged && item.Price <= 0)
            item.Price = product.PurchasePrice;


        item.VatSum = Math.Round(item.Price * item.Quantity * (product.VatRate / 100m), 2);
    }
}
