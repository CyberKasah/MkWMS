using MkWMS.Desktop.Services;
using MkWMS.Desktop.ViewModels;
using MkWMS.Desktop.Views;
using System.Windows;

namespace MkWMS.Desktop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var authService = new AuthService();
            var apiClient = new ApiClient(authService);

            var loginVm = new LoginViewModel(apiClient, authService);
            var loginWindow = new LoginWindow(loginVm);

            MainWindow = loginWindow;
            loginWindow.Show();
        }
    }
}