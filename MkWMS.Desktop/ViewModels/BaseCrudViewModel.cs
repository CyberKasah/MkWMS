using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

/// <summary>
/// Базовый CRUD ViewModel для всех справочников (товары, партии, склады и т.д.)
/// </summary>
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

    // ====================== ЗАГРУЗКА / ОБНОВЛЕНИЕ ======================

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

    // ====================== РЕДАКТИРОВАНИЕ ======================

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
        // Переопределяется в наследниках (например, для открытия окна редактирования)
    }

    // ====================== СОХРАНЕНИЕ ======================

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
                SetError("Сервер отклонил сохранение. Проверьте данные.");
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

    // ====================== УДАЛЕНИЕ ======================

    [RelayCommand]
    public virtual async Task DeleteAsync()
    {
        if (SelectedItem == null) return;

        var confirm = MessageBox.Show(
            "Вы уверены, что хотите удалить эту запись?",
            "Подтверждение удаления",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

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
                SetError("Не удалось удалить. Запись используется в других документах.");
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

    // ====================== ОТМЕНА ======================

    [RelayCommand]
    public virtual void Cancel()
    {
        SelectedItem = null;
        ClearError();
    }

    // ====================== RFID СКАНЕР ======================

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
        // Переопределяется в наследниках (например, в ProductsViewModel)
    }

    // ====================== ПАГИНАЦИЯ ======================

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

    // ====================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ======================

    protected int GetId(TDto item)
    {
        var prop = item.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
        return prop?.GetValue(item) is int id ? id : 0;
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Обновляем доступность команд при выборе элемента
        if (e.PropertyName == nameof(SelectedItem))
        {
            // Генератор делает из EditSelected -> EditSelectedCommand
            EditSelectedCommand.NotifyCanExecuteChanged();

            // КРИТИЧНО: Генератор из DeleteAsync делает DeleteCommand (убирает Async)
            DeleteCommand.NotifyCanExecuteChanged();
        }

        // Уведомляем кнопки пагинации
        if (e.PropertyName is nameof(CurrentPage) or nameof(TotalPages))
        {
            PreviousPageCommand?.NotifyCanExecuteChanged();
            NextPageCommand?.NotifyCanExecuteChanged();
        }

        // Автоматический поиск
        if (e.PropertyName == nameof(SearchText))
        {
            CurrentPage = 1;
            _ = LoadAsync();
        }
    }
    private CancellationTokenSource? _searchCts;
    partial void OnSearchTextChanged(string? value)
    {
        _searchCts?.Cancel(); // Отменяем предыдущий таймер, если пользователь всё ещё печатает
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        Task.Run(async () =>
        {
            await Task.Delay(500, token); // Ждем полсекунды
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