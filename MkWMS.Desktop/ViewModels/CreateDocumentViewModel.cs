using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.Desktop.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace MkWMS.Desktop.ViewModels
{
    public partial class CreateDocumentViewModel : BaseViewModel
    {
        private readonly ApiClient _apiClient;

        [ObservableProperty] private CreateDocumentDto document = new() { Items = new ObservableCollection<CreateDocumentItemDto>() };
        [ObservableProperty] private ObservableCollection<DocumentTypeDto> documentTypes = new();
        [ObservableProperty] private ObservableCollection<WarehouseDto> warehouses = new();
        [ObservableProperty] private ObservableCollection<ProductDto> products = new(); // добавлено для выбора товара

        public CreateDocumentViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
            _ = LoadDictionariesAsync();
        }

        private async Task LoadDictionariesAsync()
        {
            IsBusy = true;
            ClearError();

            try
            {
                var types = await _apiClient.GetDocumentTypesAsync();
                if (types != null) DocumentTypes = new ObservableCollection<DocumentTypeDto>(types);

                var req = new PagedRequestDto { Page = 1, PageSize = 100 };
                var warehousesResult = await _apiClient.GetWarehousesAsync(req);
                if (warehousesResult?.Items != null) Warehouses = new ObservableCollection<WarehouseDto>(warehousesResult.Items);

                var productsResult = await _apiClient.GetProductsAsync(req);
                if (productsResult?.Items != null) Products = new ObservableCollection<ProductDto>(productsResult.Items);
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки справочников: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void AddItem()
        {
            Document.Items.Add(new CreateDocumentItemDto { Quantity = 1 });
        }

        [RelayCommand]
        private void RemoveItem(CreateDocumentItemDto item)
        {
            if (item != null) Document.Items.Remove(item);
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (Document.Items.Count == 0)
            {
                SetError("Добавьте хотя бы одну строку товара");
                return;
            }

            IsBusy = true;
            ClearError();

            try
            {
                var id = await _apiClient.CreateDocumentAsync(Document);
                if (id.HasValue)
                {
                    MessageBox.Show($"Документ успешно создан!\nНомер: {id}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                    if (window != null) window.DialogResult = true;
                }
                else
                {
                    SetError("Не удалось создать документ");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка сохранения: {ex.Message}");
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
            if (window != null) window.DialogResult = false;
        }
    }
}