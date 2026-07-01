using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.Desktop.Models;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.Views;
using MkWMS.Desktop.Views.Dialogs;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly AuthService _authService;
    private readonly NavigationService _navigation;

    public ApiClient ApiClient => _apiClient;

    [ObservableProperty] private string login = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private bool isPasswordVisible;

    public string PasswordToggleText => IsPasswordVisible ? "Скрыть" : "Показать";

    public LoginViewModel(ApiClient apiClient, AuthService authService, NavigationService navigation)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    partial void OnIsPasswordVisibleChanged(bool value) => OnPropertyChanged(nameof(PasswordToggleText));

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
        {
            SetError("Введите логин и пароль");
            return;
        }

        IsLoading = true;
        ClearError();

        try
        {
            var (success, message, requiresChange, token, refreshToken, user) = await _apiClient.LoginAsync(Login, Password);

            if (!success || user == null)
            {
                SetError(message ?? "Неверный логин или пароль");
                return;
            }

            _authService.SetUser(user.Login, user.Roles.Select(r => r.Name).ToList(), token ?? "", refreshToken, user.WarehouseId);

            if (!_authService.Roles.Any())
            {
                SetError("У пользователя нет назначенных ролей");
                return;
            }

            if (requiresChange)
            {
                var changeVm = new ChangePasswordViewModel(_apiClient);
                var changeWindow = new ChangePasswordWindow { DataContext = changeVm };

                changeWindow.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

                if (changeWindow.ShowDialog() != true)
                {
                    SetError("Вход невозможен без смены пароля");
                    return;
                }
            }


            var dashboardVm = new DashboardViewModel(_apiClient, _authService, _navigation);
            var dashboardWindow = new DashboardWindow { DataContext = dashboardVm };

            dashboardWindow.Show();


            Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this)?.Close();
        }
        catch (Exception ex)
        {
            SetError($"Ошибка соединения: {ex.Message}");
        }
        finally { IsLoading = false; }
    }
}