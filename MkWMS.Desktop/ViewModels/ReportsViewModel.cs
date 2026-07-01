using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.Views.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class ReportsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<StockBalanceReportDto> _stockBalances = new();
    [ObservableProperty] private ObservableCollection<StockMovementReportDto> _movements = new();
    [ObservableProperty] private DateTime? _fromDate = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime? _toDate = DateTime.Today;
    public bool IsNotLoading => !IsLoading;

    public ReportsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAllAsync();
    }

    [RelayCommand]
    public async Task LoadAllAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        ClearError();

        try
        {
            var req = new PagedRequestDto { Page = 1, PageSize = 5000 };
            var balancesTask = _apiClient.GetStockBalancesReportAsync(req);

            var movementsTask = _apiClient.GetStockMovementsReportAsync(req, FromDate, ToDate);

            await Task.WhenAll(balancesTask, movementsTask);

            StockBalances = new ObservableCollection<StockBalanceReportDto>(balancesTask.Result?.Items ?? []);
            Movements = new ObservableCollection<StockMovementReportDto>(movementsTask.Result?.Items ?? []);
        }
        catch (Exception ex)
        {
            SetError($"Ошибка загрузки отчётов: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void Refresh() => _ = LoadAllAsync();


    [RelayCommand]
    private async Task ExportBalancesAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel файлы (*.xlsx)|*.xlsx",
            FileName = $"Остатки_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx"
        };

        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        try
        {
            var bytes = await _apiClient.ExportStockBalancesExcelAsync(null, null);
            if (bytes != null)
            {
                await File.WriteAllBytesAsync(dialog.FileName, bytes);
                AppMessageBoxWindow.Show("Остатки успешно экспортированы!", "Готово", AppMessageBoxIcon.Success);
            }
        }
        catch (Exception ex)
        {
            SetError($"Ошибка экспорта остатков: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ExportMovementsAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Excel файлы (*.xlsx)|*.xlsx",
            FileName = $"Движения_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx"
        };

        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        try
        {
            var req = new PagedRequestDto { Page = 1, PageSize = 10000 };
            var bytes = await _apiClient.ExportAnyReportToExcelAsync($"{ApiEndpoints.Reports}/movements", req);

            if (bytes != null)
            {
                await File.WriteAllBytesAsync(dialog.FileName, bytes);
                AppMessageBoxWindow.Show("Движения успешно экспортированы!", "Готово", AppMessageBoxIcon.Success);
            }
        }
        catch (Exception ex)
        {
            SetError($"Ошибка экспорта движений: {ex.Message}");
        }
        finally { IsLoading = false; }
    }


    [RelayCommand]
    private async Task ImportProductsAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Excel файлы (*.xlsx)|*.xlsx",
            Title = "Выберите файл для импорта товаров"
        };

        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        try
        {
            var success = await _apiClient.ImportProductsFromExcelAsync(dialog.FileName);
            if (success)
            {
                AppMessageBoxWindow.Show("Импорт товаров завершён успешно!\n\nДанные обновлены на сервере.",
                    "Готово", AppMessageBoxIcon.Success);
                await LoadAllAsync();
            }
            else
            {

                var reason = _apiClient.LastErrorMessage ?? "Сервер вернул ошибку импорта";
                SetError(reason);
                AppMessageBoxWindow.Show(reason, "Импорт не выполнен", AppMessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SetError($"Ошибка импорта: {ex.Message}");
        }
        finally { IsLoading = false; }
    }
}