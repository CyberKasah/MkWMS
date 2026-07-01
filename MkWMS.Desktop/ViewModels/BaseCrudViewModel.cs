using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.Views.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class BaseCrudViewModel<TDto> : BaseViewModel where TDto : class, new()
{
    protected readonly ApiClient _api;
    protected readonly string _endpoint;

    [ObservableProperty] private ObservableCollection<TDto> _items = new();
    [ObservableProperty] private TDto? _selectedItem;
    [ObservableProperty] private string? _searchText;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private int _totalCount = 0;

    [ObservableProperty] private string _rfidInput = string.Empty;

    public BaseCrudViewModel(ApiClient api, string endpoint)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }



    [RelayCommand]
    public virtual async Task LoadAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ClearError();

        try
        {
            var req = new PagedRequestDto
            {
                Page = CurrentPage,
                PageSize = 20,
                Search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim()
            };

            var result = await _api.GetPagedAsync<TDto>(_endpoint, req);

            if (result == null)
            {
                SetError($"Сервер не ответил по адресу '{_endpoint}'");
                return;
            }

            TotalPages = Math.Max(1, result.TotalPages);
            TotalCount = result.TotalCount;
            CurrentPage = Math.Max(1, result.Page);

            Items.Clear();
            if (result.Items != null)
            {
                foreach (var item in result.Items)
                    Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            SetError($"Ошибка загрузки: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public virtual async Task Refresh() => await LoadAsync();



    [RelayCommand]
    public virtual void EditSelected()
    {
        if (SelectedItem != null)
        {
            OnEditSelected(SelectedItem);
        }
    }

    protected virtual void OnEditSelected(TDto item)
    {

    }



    [RelayCommand]
    public virtual async Task SaveAsync()
    {
        if (SelectedItem == null) return;
        if (!Validate()) return;

        int id = GetId(SelectedItem);
        IsLoading = true;
        ClearError();

        try
        {
            bool success = id == 0
                ? await _api.CreateAsync(_endpoint, SelectedItem) != null
                : await _api.UpdateAsync(_endpoint, id, SelectedItem);

            if (success)
            {
                SelectedItem = null;
                await LoadAsync();
            }
            else
            {



                var reason = _api.LastErrorMessage ?? "Сервер отклонил сохранение. Проверьте данные.";
                SetError(reason);
                AppMessageBoxWindow.Show(reason, "Не удалось сохранить", AppMessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SetError($"Критическая ошибка сохранения: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }



    [RelayCommand]
    public virtual async Task DeleteAsync()
    {
        if (SelectedItem == null) return;

        var confirmed = AppMessageBoxWindow.Confirm(
            "Вы уверены, что хотите удалить эту запись?",
            "Подтверждение удаления");

        if (!confirmed) return;

        int id = GetId(SelectedItem);
        if (id == 0) return;

        IsLoading = true;
        ClearError();

        try
        {
            var success = await _api.DeleteAsync(_endpoint, id);
            if (success)
            {
                SelectedItem = null;
                await LoadAsync();
            }
            else
            {
                var reason = _api.LastErrorMessage ?? "Не удалось удалить. Запись используется в других документах.";
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



    [RelayCommand]
    public virtual void Cancel()
    {
        SelectedItem = null;
        ClearError();
    }



    partial void OnRfidInputChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        if (value.Contains("\n") || value.Contains("\r"))
        {
            string cleanRfid = value.Trim();
            RfidInput = string.Empty;
            OnRfidScanned(cleanRfid);
        }
    }

    protected virtual void OnRfidScanned(string rfid)
    {

    }



    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    public async Task PreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadAsync();
        }
    }
    private bool CanGoPrevious => CurrentPage > 1;

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    public async Task NextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadAsync();
        }
    }
    private bool CanGoNext => CurrentPage < TotalPages;



    protected int GetId(TDto item)
    {
        var prop = item.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item) is int id ? id : 0;
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);


        if (e.PropertyName == nameof(SelectedItem))
        {

            EditSelectedCommand.NotifyCanExecuteChanged();


            DeleteCommand.NotifyCanExecuteChanged();
        }


        if (e.PropertyName is nameof(CurrentPage) or nameof(TotalPages))
        {
            PreviousPageCommand?.NotifyCanExecuteChanged();
            NextPageCommand?.NotifyCanExecuteChanged();
        }


        if (e.PropertyName == nameof(SearchText))
        {
            CurrentPage = 1;
            _ = LoadAsync();
        }
    }
    private CancellationTokenSource? _searchCts;
    partial void OnSearchTextChanged(string? value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Run(async () =>
        {
            await Task.Delay(500, token);
            if (!token.IsCancellationRequested)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentPage = 1;
                    _ = LoadAsync();
                });
            }
        }, token);
    }
}