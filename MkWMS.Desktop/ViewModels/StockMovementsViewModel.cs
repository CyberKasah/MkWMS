using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MkWMS.Desktop.ViewModels;

public partial class StockMovementsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<StockMovementReportDto> _movements = new();
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount = 0;

    public bool IsNotLoading => !IsLoading;

    public StockMovementsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        ClearError();

        try
        {
            var req = new PagedRequestDto
            {
                Page = CurrentPage,
                PageSize = 50,
                Search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim()
            };

            var result = await _apiClient.GetStockMovementsReportAsync(req);

            if (result != null)
            {
                TotalPages = Math.Max(1, result.TotalPages);
                TotalCount = result.TotalCount;
                CurrentPage = Math.Max(1, result.Page);

                Movements = new ObservableCollection<StockMovementReportDto>(result.Items ?? []);
            }
        }
        catch (Exception ex)
        {
            SetError($"Ошибка загрузки движений: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Refresh() => _ = LoadAsync();

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    public async Task PreviousPageAsync() { CurrentPage--; await LoadAsync(); }
    private bool CanGoPrevious => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    public async Task NextPageAsync() { CurrentPage++; await LoadAsync(); }
    private bool CanGoNext => CurrentPage < TotalPages;

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(CurrentPage) or nameof(TotalPages))
        {
            PreviousPageCommand.NotifyCanExecuteChanged();
            NextPageCommand.NotifyCanExecuteChanged();
        }
    }
}