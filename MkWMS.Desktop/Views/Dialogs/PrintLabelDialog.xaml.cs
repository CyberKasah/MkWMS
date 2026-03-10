using System.Windows;
using MkWMS.Desktop.ViewModels;

namespace MkWMS.Desktop.Views.Dialogs;

public partial class PrintLabelDialog : Window
{
    public PrintLabelDialog(PrintLabelViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}