using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace MkWMS.Desktop.ViewModels;

public partial class AuditLogsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<AuditLogDto> _items = new();
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private AuditLogDto? _selectedLog;
    [ObservableProperty] private ObservableCollection<ChangeDetail> _selectedLogDetails = new();

    public class ChangeDetail
    {
        public string Field { get; set; } = string.Empty;
        public string Old { get; set; } = "—";
        public string New { get; set; } = "—";
    }

    public AuditLogsViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _ = Load();
    }

    partial void OnSelectedLogChanged(AuditLogDto? value)
    {
        SelectedLogDetails.Clear();
        if (value == null) return;

        if (!string.IsNullOrWhiteSpace(value.ChangesJson))
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var json = value.ChangesJson.Trim();

                // Если JSON корректный и это массив
                if (json.StartsWith("[") && json.EndsWith("]"))
                {
                    var details = JsonSerializer.Deserialize<List<ChangeDetail>>(json, options);
                    if (details != null)
                    {
                        foreach (var d in details)
                        {
                            // Пропускаем технические поля
                            if (d.Field == "Id" || d.Field.EndsWith("Id") || d.Field.Contains("Navigation"))
                                continue;

                            SelectedLogDetails.Add(d);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Чтобы вы видели ошибку в отладке
                System.Diagnostics.Debug.WriteLine($"JSON Error: {ex.Message}");
                SelectedLogDetails.Add(new ChangeDetail { Field = "Ошибка", New = "Не удалось прочитать изменения" });
            }
        }

        // Если изменений нет, можно добавить строку-заглушку
        if (SelectedLogDetails.Count == 0 && !string.IsNullOrEmpty(value.Action))
        {
            SelectedLogDetails.Add(new ChangeDetail { Field = "Инфо", New = "Нет зафиксированных изменений полей" });
        }
    }

    [RelayCommand]
    public async Task Load()
    {
        if (IsLoading) return;
        IsLoading = true;
        ClearError();
        SelectedLog = null;

        try
        {
            var req = new PagedRequestDto
            {
                Page = 1,
                PageSize = 200,
                Search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim()
            };

            var result = await _apiClient.GetAuditLogsAsync(req);
            Items.Clear();
            if (result?.Items != null)
            {
                foreach (var log in result.Items)
                    Items.Add(log);
            }
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task Refresh() => await Load();

    private System.Threading.CancellationTokenSource? _searchCts;

    partial void OnSearchTextChanged(string value)
    {
        // Отменяем предыдущий таймер, если пользователь продолжает печатать
        _searchCts?.Cancel();
        _searchCts = new System.Threading.CancellationTokenSource();
        var token = _searchCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                // Ждем 500 мс после последнего нажатия клавиши
                await Task.Delay(500, token);

                if (!token.IsCancellationRequested)
                {
                    // Обязательно вызываем Load() в главном UI-потоке
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        _ = Load();
                    });
                }
            }
            catch (TaskCanceledException) { /* Игнорируем, это штатная отмена */ }
        });
    }
}