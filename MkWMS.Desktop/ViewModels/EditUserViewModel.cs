using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Models;
using MkWMS.Desktop.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MkWMS.Desktop.ViewModels;

public partial class EditUserViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    public bool IsNew { get; }

    [ObservableProperty]
    private UserDto user = new();

    public ObservableCollection<WarehouseDto> Warehouses { get; set; } = new();

    public ObservableCollection<RoleDto> Roles { get; set; } = new();

    // Ссылка на ListBox для доступа к выбранным ролям
    public ListBox? RolesListBox { get; set; }

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    // Конструктор для редактирования
    public EditUserViewModel(ApiClient apiClient, UserDto existingUser)
    {
        _apiClient = apiClient;
        IsNew = false;

        User = new UserDto
        {
            Id = existingUser.Id,
            Login = existingUser.Login,
            FullName = existingUser.FullName ?? "",
            IsActive = existingUser.IsActive ?? true,
            WarehouseId = existingUser.WarehouseId
        };

        _ = LoadDataAsync();
    }

    // Конструктор для создания нового
    public EditUserViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        IsNew = true;

        User = new UserDto
        {
            IsActive = true
        };

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            // Склады
            var req = new PagedRequestDto { Page = 1, PageSize = 100 };
            var warehouses = await _apiClient.GetWarehousesAsync(req);
            if (warehouses?.Items != null)
            {
                Warehouses = new ObservableCollection<WarehouseDto>(warehouses.Items);
            }

            // Роли
            var roles = await _apiClient.GetAllRolesAsync();
            if (roles != null)
            {
                Roles = new ObservableCollection<RoleDto>(roles);
            }
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            if (string.IsNullOrWhiteSpace(User.Login))
            {
                ErrorMessage = "Логин обязателен";
                return;
            }

            if (string.IsNullOrWhiteSpace(User.FullName))
            {
                ErrorMessage = "ФИО обязательно";
                return;
            }

            var selectedRoleIds = RolesListBox?.SelectedItems
                .Cast<RoleDto>()
                .Select(r => r.Id)
                .Distinct()
                .ToList() ?? new List<int>();

            if (!selectedRoleIds.Any())
            {
                ErrorMessage = "Выберите хотя бы одну роль";
                return;
            }

            if (IsNew)
            {
                var createDto = new CreateUserWithRolesDto
                {
                    Login = User.Login,
                    Password = "temp123", // ← временный пароль, если поле нет в XAML — замени на реальное или сделай поле
                    FullName = User.FullName,
                    RoleIds = selectedRoleIds,
                    WarehouseId = User.WarehouseId
                };

                var created = await _apiClient.CreateUserAsync(createDto);
                if (created == null)
                {
                    ErrorMessage = "Не удалось создать пользователя";
                    return;
                }
            }
            else
            {
                var updateDto = new UpdateUserDto
                {
                    FullName = User.FullName,
                    IsActive = User.IsActive,
                    WarehouseId = User.WarehouseId
                };

                var updated = await _apiClient.UpdateUserAsync(User.Id, updateDto);
                if (!updated)
                {
                    ErrorMessage = "Не удалось обновить пользователя";
                    return;
                }

                var rolesAssigned = await _apiClient.AssignRolesAsync(User.Id, selectedRoleIds);
                if (!rolesAssigned)
                {
                    ErrorMessage = "Не удалось обновить роли";
                    return;
                }
            }

            var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }
        catch (System.Exception ex)
        {
            ErrorMessage = ex.Message;
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
        if (window != null)
        {
            window.DialogResult = false;
            window.Close();
        }
    }
}