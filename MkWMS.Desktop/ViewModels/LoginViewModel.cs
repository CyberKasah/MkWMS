using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly AuthService _authService;

    public ApiClient ApiClient => _apiClient;

    [ObservableProperty]
    private string login = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isPasswordVisible = false;

    public string PasswordToggleText =>
        IsPasswordVisible ? "Скрыть пароль" : "Показать пароль";

    partial void OnIsPasswordVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(PasswordToggleText));
    }

    public LoginViewModel(ApiClient apiClient, AuthService authService)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    [RelayCommand]
    public void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        // Проверка пустых полей
        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
        {
            MessageBox.Show(
                "Введите логин и пароль",
                "Ошибка входа",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            SetError("Введите логин и пароль");
            return;
        }

        IsBusy = true;
        ClearError();

        try
        {
            var (success, message, requiresChange, token, user) =
                await _apiClient.LoginAsync(Login, Password);

            if (!success)
            {
                string errorText = message ?? "Неверный логин или пароль. Проверьте введённые данные.";
                MessageBox.Show(
                    errorText,
                    "Ошибка входа",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                SetError(errorText);
                return;
            }

            if (user == null)
            {
                MessageBox.Show(
                    "Ошибка получения данных пользователя от сервера. Попробуйте позже.",
                    "Критическая ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                SetError("Ошибка получения данных пользователя");
                return;
            }

            // Установка пользователя и ролей
            _authService.SetUser(
                user.Login,
                user.Roles.Select(r => r.Name).ToList(),
                token ?? "",
                user.WarehouseId
            );

            // Проверка ролей (для отладки и уверенности)
            if (!_authService.Roles.Any())
            {
                MessageBox.Show(
                    "У пользователя нет ни одной роли. Обратитесь к администратору системы.",
                    "Ошибка прав доступа",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            // Если требуется смена пароля
            if (requiresChange)
            {
                var changeVm = new ChangePasswordViewModel(_apiClient);
                var changeWindow = new ChangePasswordWindow
                {
                    DataContext = changeVm,
                    Owner = Application.Current.MainWindow
                };

                if (changeWindow.ShowDialog() != true)
                {
                    MessageBox.Show(
                        "Смена пароля отменена. Вход невозможен без смены пароля.",
                        "Вход отменён",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }
            }

            // Успешный вход → открываем дашборд
            var navigation = new NavigationService();
            var dashboardVm = new DashboardViewModel(_apiClient, _authService, navigation);
            var dashboard = new DashboardWindow
            {
                DataContext = dashboardVm
            };

            Application.Current.MainWindow = dashboard;
            dashboard.Show();

            Application.Current.Windows
                .OfType<LoginWindow>()
                .FirstOrDefault()
                ?.Close();
        }
        catch (Exception ex)
        {
            string errorMsg = $"Произошла непредвиденная ошибка при входе:\n{ex.Message}";
            MessageBox.Show(
                errorMsg,
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            SetError($"Ошибка входа: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}