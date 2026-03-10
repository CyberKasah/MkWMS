// ViewModels/ProductsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Data.Entities;
using MkWMS.Desktop.Models;
using MkWMS.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class ProductsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<ProductDto> products = new();

    [ObservableProperty]
    private ProductDto? selectedProduct;

    [ObservableProperty]
    private string searchText = string.Empty;

    public ProductsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ClearError();

        try
        {
            var req = new PagedRequestDto
            {
                Page = 1,
                PageSize = 100,
                Search = SearchText
            };

            var result = await _apiClient.GetProductsAsync(req);
            Products = new ObservableCollection<ProductDto>(result?.Items ?? []);   
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Refresh() => LoadAsync();

    [RelayCommand]
    private void PrintLabel()
    {
        if (SelectedProduct == null)
        {
            SetError("Выберите товар для печати этикетки");
            return;
        }

        try
        {
            var url = $"https://localhost:7000/api/products/label/{SelectedProduct.Id}?qty=3";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SetError("Не удалось открыть этикетку: " + ex.Message);
        }
    }


    [RelayCommand]
    private void CreateNew() => MessageBox.Show("Создание нового товара будет добавлено позже", "Инфо");

    [RelayCommand]
    private void Edit() => MessageBox.Show("Редактирование товара будет добавлено позже", "Инфо");

    [RelayCommand]
    private void Delete() => MessageBox.Show("Удаление товара будет добавлено позже", "Инфо");
}