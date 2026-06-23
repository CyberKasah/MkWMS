using MkWMS.Desktop.ViewModels;
using System.Windows;

namespace MkWMS.Desktop.Views.Dialogs
{
    public partial class PrintLabelDialog : Window
    {
        public PrintLabelDialog(PrintLabelViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;   // ← это ключевая строка!
        }
    }
}