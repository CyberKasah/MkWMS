using MkWMS.Desktop.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MkWMS.Desktop.Views
{
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        private void OldPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordViewModel vm && sender is PasswordBox pb)
            {
                vm.OldPassword = pb.Password;
            }
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordViewModel vm && sender is PasswordBox pb)
            {
                vm.NewPassword = pb.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChangePasswordViewModel vm && sender is PasswordBox pb)
            {
                vm.ConfirmNewPassword = pb.Password;
            }
        }
    }
}