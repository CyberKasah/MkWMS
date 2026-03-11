using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.Views;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;


namespace MkWMS.Desktop.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly AuthService _authService;
    private readonly NavigationService _navigation;
    [ObservableProperty] private BaseViewModel currentViewModel = null!;
    [ObservableProperty] private string currentUserName = "Пользователь";
    public ObservableCollection<string> Roles { get; set; } = new();
    public DashboardViewModel(ApiClient apiClient, AuthService authService, NavigationService navigation)
    {
        _apiClient = apiClient;
        _authService = authService;
        _navigation = navigation;
        Roles = new ObservableCollection<string>(_authService.Roles);
        CurrentUserName = _authService.Login ?? "Пользователь";
        _navigation.ViewModelChanged += () =>
        {
            CurrentViewModel = _navigation.CurrentViewModel!;
        };
        ConfigureDashboardByRole();
        _ = LoadDashboardDataAsync();
    }
    private void ConfigureDashboardByRole()
    {
        if (_authService.IsAdministrator)
        {
            _navigation.Navigate(new UsersViewModel(_apiClient));
        }
        else if (_authService.IsRukovoditel)
        {
            _navigation.Navigate(new ReportsViewModel(_apiClient));
        }
        else if (_authService.IsKladovschik)
        {
            _navigation.Navigate(new ProductsViewModel(_apiClient));
        }
        else
        {
            MessageBox.Show("У вас нет достаточных прав для использования системы.", "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Error);
            Logout();
        }
    }
    private async Task LoadDashboardDataAsync()
    {
        await Task.CompletedTask;
    }
    [RelayCommand]
    private void Logout()
    {
        _authService.Logout();
        var loginVm = new LoginViewModel(_apiClient, _authService);
        var loginWindow = new LoginWindow(loginVm);
        loginWindow.Show();
        Application.Current.MainWindow?.Close();
    }
    [RelayCommand] public void GoToDashboard() => ConfigureDashboardByRole();
    [RelayCommand] public void GoToProducts() => _navigation.Navigate(new ProductsViewModel(_apiClient));
    [RelayCommand] public void GoToDocuments() => _navigation.Navigate(new DocumentsViewModel(_apiClient));
    [RelayCommand] public void GoToUsers() => _navigation.Navigate(new UsersViewModel(_apiClient));
    [RelayCommand] public void GoToWarehouses() => _navigation.Navigate(new WarehousesViewModel(_apiClient));
    [RelayCommand] public void GoToReports() => _navigation.Navigate(new ReportsViewModel(_apiClient));
    [RelayCommand] public void GoToAudit() => _navigation.Navigate(new AuditLogsViewModel(_apiClient));
    [RelayCommand] public void GoToBatches() => _navigation.Navigate(new BatchesViewModel(_apiClient));
    [RelayCommand] public void GoToSerials() => _navigation.Navigate(new SerialNumbersViewModel(_apiClient));
    [RelayCommand] public void GoToDepartments() => _navigation.Navigate(new DepartmentsViewModel(_apiClient));
    [RelayCommand] public void GoToDocumentTypes() => _navigation.Navigate(new DocumentTypesViewModel(_apiClient));
    [RelayCommand] public void GoToRoles() => _navigation.Navigate(new RolesViewModel(_apiClient));
}
