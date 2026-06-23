using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MkWMS.Desktop.Views.UserControls
{
    /// <summary>
    /// Логика взаимодействия для DocumentsView.xaml
    /// </summary>
    public partial class DocumentsView : UserControl
    {
        public DocumentsView()
        {
            InitializeComponent();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null && btn.ContextMenu != null)
            {
                // Устанавливаем цель размещения, чтобы меню знало, где оно находится
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;

                // Открываем меню
                btn.ContextMenu.IsOpen = true;
            }
        }
    }
}
