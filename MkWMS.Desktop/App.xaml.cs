// App.xaml.cs — НОВЫЙ ПОЛНЫЙ ВАРИАНТ
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

            // 1. Регистрируем сервисы (уже было)
            services.AddSingleton<AuthService>();
            services.AddSingleton<NavigationService>();
            services.AddSingleton<ApiClient>();


            // 2. РЕГИСТРИРУЕМ ВЬЮ-МОДЕЛИ (Этого не хватало!)
            // Рекомендуется использовать AddTransient для VM окон, 
            // чтобы при каждом запросе создавался новый экземпляр
            services.AddTransient<LoginViewModel>();
            Services = services.BuildServiceProvider();

            // Теперь GetRequiredService найдет LoginViewModel
            var loginVm = Services.GetRequiredService<LoginViewModel>();
            var loginWindow = new LoginWindow(loginVm);
            MainWindow = loginWindow;
            loginWindow.Show();
        }
    }
}