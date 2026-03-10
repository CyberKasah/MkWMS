// ViewModels/ReportsViewModel.cs
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

public partial class ReportsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<StockBalanceReportDto> stockBalances = new();

    [ObservableProperty]
    private ObservableCollection<StockMovementReportDto> movements = new();

    public ReportsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAllAsync();
    }

    [RelayCommand]
    private async Task LoadAllAsync()
    {
        IsBusy = true;
        ClearError();

        try
        {
            var balancesTask = _apiClient.GetStockBalancesReportAsync(new PagedRequestDto { Page = 1, PageSize = 100 });
            var movementsTask = _apiClient.GetStockMovementsReportAsync(new PagedRequestDto { Page = 1, PageSize = 100 });

            await Task.WhenAll(balancesTask, movementsTask);

            StockBalances = new ObservableCollection<StockBalanceReportDto>((await balancesTask)?.Items ?? Enumerable.Empty<StockBalanceReportDto>());
            Movements = new ObservableCollection<StockMovementReportDto>((await movementsTask)?.Items ?? Enumerable.Empty<StockMovementReportDto>());
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
    private void Refresh() => LoadAllAsync();
}