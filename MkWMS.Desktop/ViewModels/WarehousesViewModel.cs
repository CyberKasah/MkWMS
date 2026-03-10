// ViewModels/WarehousesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Data.Entities;
using MkWMS.Desktop.Models;
using MkWMS.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MkWMS.Desktop.ViewModels;

public partial class WarehousesViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<WarehouseDto> warehouses = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    public WarehousesViewModel(ApiClient apiClient)
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
                PageSize = 50,
                Search = SearchText
            };

            var result = await _apiClient.GetWarehousesAsync(req);
            Warehouses = new ObservableCollection<WarehouseDto>(result?.Items ?? []);
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
}