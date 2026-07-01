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


        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.IsPasswordVisible))
            {
                if (!viewModel.IsPasswordVisible && PasswordControl.Password != viewModel.Password)
                {
                    PasswordControl.Password = viewModel.Password;
                }
            }
        };
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {

            if (!vm.IsPasswordVisible)
            {
                vm.Password = PasswordControl.Password;
            }
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
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            changeWindow.ShowDialog();
        }
    }
}