using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.Views;
using MkWMS.Desktop.Views.Dialogs;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly AuthService _authService;
    private readonly NavigationService _navigation;

    [ObservableProperty] private BaseViewModel? currentViewModel;
    [ObservableProperty] private string currentUserName = "Пользователь";
    [ObservableProperty] private ObservableCollection<string> roles = new();

    public DashboardViewModel(ApiClient apiClient, AuthService authService, NavigationService navigation)
    {
        _apiClient = apiClient;
        _authService = authService;
        _navigation = navigation;

        Roles = new ObservableCollection<string>(_authService.Roles);
        CurrentUserName = _authService.Login ?? "Пользователь";

        _navigation.ViewModelChanged += () => CurrentViewModel = _navigation.CurrentViewModel;

        ConfigureDashboardByRole();
    }

    private void ConfigureDashboardByRole()
    {
        if (_authService.IsAdministrator) GoToUsers();
        else if (_authService.IsRukovoditel) GoToReports();
        else if (_authService.IsKladovschik) GoToProducts();
        else
        {
            AppMessageBoxWindow.Show("Нет прав доступа.", "Ошибка", AppMessageBoxIcon.Error);
            Logout();
        }
    }

    [RelayCommand]
    private async Task Logout()
    {
        try { await _apiClient.LogoutAsync(); }
        catch {  }

        _authService.Logout();

        var loginVm = new LoginViewModel(_apiClient, _authService, _navigation);
        var loginWindow = new LoginWindow(loginVm);

        loginWindow.Show();

        Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.DataContext == this)?.Close();
    }






    [RelayCommand]
    public void GoToHome() => ConfigureDashboardByRole();

    [RelayCommand]
    public void GoToProducts()
    {

        var batches = new BatchesViewModel(_apiClient);
        var serials = new SerialNumbersViewModel(_apiClient);
        _navigation.Navigate(new ProductsViewModel(_apiClient, batches, serials));
    }

    [RelayCommand] public void GoToUsers() => _navigation.Navigate(new UsersViewModel(_apiClient));
    [RelayCommand]
    public void GoToDocuments()
    {
        var counterpartiesVM = new CounterpartiesViewModel(_apiClient);

        _navigation.Navigate(
            new DocumentsViewModel(_apiClient, _navigation, counterpartiesVM)
        );
    }
    [RelayCommand] public void GoToReports() => _navigation.Navigate(new ReportsViewModel(_apiClient));
    [RelayCommand] public void GoToAudit() => _navigation.Navigate(new AuditLogsViewModel(_apiClient));
    [RelayCommand] public void GoToWarehouses() => _navigation.Navigate(new WarehousesViewModel(_apiClient));
    [RelayCommand] public void GoToBatches() => _navigation.Navigate(new BatchesViewModel(_apiClient));
    [RelayCommand] public void GoToSerials() => _navigation.Navigate(new SerialNumbersViewModel(_apiClient));
    [RelayCommand] public void GoToDepartments() => _navigation.Navigate(new DepartmentsViewModel(_apiClient));
    [RelayCommand] public void GoToCounterparties() => _navigation.Navigate(new CounterpartiesViewModel(_apiClient));
    [RelayCommand] public void GoToRoles() => _navigation.Navigate(new RolesViewModel(_apiClient));
}