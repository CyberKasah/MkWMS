using MkWMS.Desktop.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace MkWMS.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Password = PasswordBox.Password;
        }
    }

    private void ForgotPassword_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is LoginViewModel loginVm)
        {
            var changeVm = new ChangePasswordViewModel(loginVm.ApiClient);
            var changeWindow = new ChangePasswordWindow
            {
                DataContext = changeVm,
                Owner = this,
                WindowState = WindowState.Maximized
            };
            changeWindow.ShowDialog();
        }
    }
}