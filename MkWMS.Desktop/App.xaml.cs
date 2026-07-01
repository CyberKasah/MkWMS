
using Microsoft.Extensions.DependencyInjection;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.ViewModels;
using MkWMS.Desktop.Views;
using System.Windows;

namespace MkWMS.Desktop
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();


            services.AddSingleton<AuthService>();
            services.AddSingleton<NavigationService>();
            services.AddSingleton<ApiClient>();




            services.AddTransient<LoginViewModel>();
            Services = services.BuildServiceProvider();


            var loginVm = Services.GetRequiredService<LoginViewModel>();
            var loginWindow = new LoginWindow(loginVm);
            MainWindow = loginWindow;
            loginWindow.Show();
        }
    }
}