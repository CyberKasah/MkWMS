// ViewModels/ChangePasswordViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.Desktop.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace MkWMS.Desktop.ViewModels;

public partial class ChangePasswordViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private string oldPassword = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private string confirmNewPassword = string.Empty;

    public ChangePasswordViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (NewPassword != ConfirmNewPassword)
        {
            SetError("Пароли не совпадают");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            SetError("Новый пароль не может быть пустым");
            return;
        }

        IsBusy = true;
        ClearError();

        try
        {
            var success = await _apiClient.ChangePasswordAsync(OldPassword, NewPassword);
            if (success)
            {
                MessageBox.Show("Пароль успешно изменён!", "Готово",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                var window = Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.DataContext == this);
                if (window != null) window.DialogResult = true;
            }
            else
                SetError("Не удалось сменить пароль");
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
    private void Cancel()
    {
        var window = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.DataContext == this);
        if (window != null) window.DialogResult = false;
    }
}