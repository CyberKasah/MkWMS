using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.Desktop.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class ChangePasswordViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty]
    private string oldPassword = string.Empty;

    [ObservableProperty]
    private string newPassword = string.Empty;

    [ObservableProperty]
    private string confirmNewPassword = string.Empty;

    // ── Показывать пароль как текст ────────────────────────────────
    [ObservableProperty]
    private bool isOldPasswordVisible;

    [ObservableProperty]
    private bool isNewPasswordVisible;

    [ObservableProperty]
    private bool isConfirmPasswordVisible;

    public ChangePasswordViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    // ── Команды переключения видимости пароля ──────────────────────

    [RelayCommand]
    private void ToggleOldPasswordVisibility()
    {
        IsOldPasswordVisible = !IsOldPasswordVisible;
    }

    [RelayCommand]
    private void ToggleNewPasswordVisibility()
    {
        IsNewPasswordVisible = !IsNewPasswordVisible;
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
    }

    // ── Основная команда смены пароля ──────────────────────────────

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (NewPassword != ConfirmNewPassword)
        {
            SetError("Пароли не совпадают");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            SetError("Новый пароль должен быть не менее 6 символов");
            return;
        }

        IsLoading = true;
        ClearError();

        try
        {
            var success = await _apiClient.ChangePasswordAsync(OldPassword, NewPassword);

            if (success)
            {
                MessageBox.Show("Пароль успешно изменён!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                CloseWindow(true);
            }
            else
            {
                SetError("Неверный старый пароль или ошибка сервера");
            }
        }
        catch (Exception ex)
        {
            SetError($"Ошибка: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseWindow(false);
    }

    private void CloseWindow(bool? dialogResult)
    {
        var window = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.DataContext == this);

        if (window != null)
        {
            window.DialogResult = dialogResult;
            window.Close();
        }
    }
}