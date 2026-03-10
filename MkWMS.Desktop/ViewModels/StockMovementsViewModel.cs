// ViewModels/StockMovementsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Models;
using MkWMS.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MkWMS.Desktop.ViewModels;

public partial class StockMovementsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<StockMovementReportDto> movements = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    public StockMovementsViewModel(ApiClient apiClient)
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

            var result = await _apiClient.GetStockMovementsReportAsync(req);
            Movements = new ObservableCollection<StockMovementReportDto>(result?.Items ?? []);
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