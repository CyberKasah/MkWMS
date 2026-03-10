using System.Windows;
using MkWMS.Desktop.ViewModels;

namespace MkWMS.Desktop.Views.Dialogs;

public partial class EditUserDialog : Window
{
    public EditUserDialog(EditUserViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}