using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class EditUserViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    public bool IsNew { get; }

    [ObservableProperty]
    private UserDto _user;

    [ObservableProperty]
    private string _password = string.Empty;




    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _newPasswordConfirm = string.Empty;

    [ObservableProperty]
    private string _adminConfirmPassword = string.Empty;


    [ObservableProperty]
    private string? _resetPasswordMessage;

    [ObservableProperty]
    private bool _resetPasswordSuccess;

    public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
    public ObservableCollection<RoleSelection> Roles { get; } = new();

    public EditUserViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        IsNew = true;
        _user = new UserDto { IsActive = true };
        _ = LoadDataAsync();
    }

    public EditUserViewModel(ApiClient apiClient, UserDto existingUser)
    {
        _apiClient = apiClient;
        IsNew = false;


        _user = new UserDto
        {
            Id = existingUser.Id,
            Login = existingUser.Login,
            FullName = existingUser.FullName,
            IsActive = existingUser.IsActive,
            WarehouseId = existingUser.WarehouseId,
            Roles = existingUser.Roles?.ToList() ?? new List<RoleDto>()
        };

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        ClearError();

        try
        {
            var warehousesTask = _apiClient.GetAllWarehousesAsync();
            var rolesTask = _apiClient.GetRolesAsync();

            await Task.WhenAll(warehousesTask, rolesTask);

            Warehouses.Clear();
            var warehouses = await warehousesTask;
            if (warehouses != null)
            {
                foreach (var w in warehouses) Warehouses.Add(w);
            }

            Roles.Clear();
            var allRoles = await rolesTask ?? new List<RoleDto>();
            foreach (var role in allRoles)
            {
                var isSelected = User.Roles?.Any(ur => ur.Id == role.Id) ?? false;
                Roles.Add(new RoleSelection { Role = role, IsSelected = isSelected });
            }
        }
        catch (Exception ex) { SetError($"Ошибка загрузки: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate()) return;

        IsLoading = true;
        ClearError();

        try
        {
            var selectedRoleIds = Roles.Where(r => r.IsSelected).Select(r => r.Role.Id).ToList();
            bool success;

            if (IsNew)
            {
                var createDto = new CreateUserWithRolesDto
                {
                    Login = User.Login,
                    Password = Password,
                    FullName = User.FullName,
                    WarehouseId = User.WarehouseId,
                    RoleIds = selectedRoleIds
                };
                var result = await _apiClient.CreateUserAsync(createDto);
                success = result != null;
            }
            else
            {
                var updateDto = new UpdateUserDto
                {
                    FullName = User.FullName,
                    IsActive = User.IsActive,
                    WarehouseId = User.WarehouseId
                };

                var updateTask = _apiClient.UpdateUserAsync(User.Id, updateDto);
                var rolesTask = _apiClient.AssignRolesAsync(User.Id, selectedRoleIds);

                await Task.WhenAll(updateTask, rolesTask);
                success = await updateTask && await rolesTask;
            }

            if (success) CloseWithResult(true);
            else SetError("Сервер отклонил сохранение.");
        }
        catch (Exception ex) { SetError($"Ошибка API: {ex.Message}"); }
        finally { IsLoading = false; }
    }

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(User.Login)) { SetError("Логин обязателен"); return false; }
        if (string.IsNullOrWhiteSpace(User.FullName)) { SetError("ФИО обязательно"); return false; }
        if (IsNew && string.IsNullOrWhiteSpace(Password)) { SetError("Пароль обязателен"); return false; }
        if (!Roles.Any(r => r.IsSelected)) { SetError("Выберите хотя бы одну роль"); return false; }
        return true;
    }

    [RelayCommand]
    private void Cancel() => CloseWithResult(false);




    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        ResetPasswordMessage = null;
        ResetPasswordSuccess = false;

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 4)
        {
            ResetPasswordMessage = "Новый пароль должен содержать минимум 4 символа";
            return;
        }
        if (NewPassword != NewPasswordConfirm)
        {
            ResetPasswordMessage = "Пароли не совпадают";
            return;
        }
        if (string.IsNullOrWhiteSpace(AdminConfirmPassword))
        {
            ResetPasswordMessage = "Введите свой пароль для подтверждения";
            return;
        }

        IsLoading = true;
        try
        {
            var (success, message) = await _apiClient.ResetUserPasswordAsync(User.Id, NewPassword, AdminConfirmPassword);
            ResetPasswordSuccess = success;
            ResetPasswordMessage = success
                ? $"Пароль пользователя «{User.Login}» успешно изменён"
                : (message ?? "Не удалось сбросить пароль");

            if (success)
            {
                NewPassword = string.Empty;
                NewPasswordConfirm = string.Empty;
                AdminConfirmPassword = string.Empty;
            }
        }
        catch (Exception ex)
        {
            ResetPasswordSuccess = false;
            ResetPasswordMessage = $"Ошибка: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    private void CloseWithResult(bool result)
    {
        var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
        if (window != null)
        {
            try { window.DialogResult = result; } catch { }
            window.Close();
        }
    }
}

public partial class RoleSelection : ObservableObject
{
    public required RoleDto Role { get; set; }
    [ObservableProperty] private bool isSelected;
}