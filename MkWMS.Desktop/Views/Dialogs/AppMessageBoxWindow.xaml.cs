using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace MkWMS.Desktop.Views.Dialogs
{
    public enum AppMessageBoxIcon { Info, Success, Warning, Error, Question }








    public partial class AppMessageBoxWindow : Window
    {
        private bool _result;

        public AppMessageBoxWindow()
        {
            InitializeComponent();
        }

        private void ApplyIcon(AppMessageBoxIcon icon)
        {
            var (glyph, brushKey) = icon switch
            {
                AppMessageBoxIcon.Success => ("✓", "Success"),
                AppMessageBoxIcon.Warning => ("!", "Warning"),
                AppMessageBoxIcon.Error => ("✕", "Danger"),
                AppMessageBoxIcon.Question => ("?", "Primary"),
                _ => ("i", "Primary")
            };

            var brush = (Brush)(Application.Current.TryFindResource(brushKey) ?? Brushes.SlateGray);
            IconBadge.Background = new SolidColorBrush(((SolidColorBrush)brush).Color) { Opacity = 0.15 };
            IconGlyph.Text = glyph;
            IconGlyph.Foreground = brush;
            IconGlyph.FontWeight = FontWeights.Bold;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _result = true;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result = false;
            DialogResult = false;
        }

        private static Window? FindOwner()
        {
            return Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                   ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault();
        }


        public static void Show(string message, string title = "Сообщение", AppMessageBoxIcon icon = AppMessageBoxIcon.Info)
        {
            var win = new AppMessageBoxWindow();
            win.TitleBlock.Text = title;
            win.MessageBlock.Text = message;
            win.ApplyIcon(icon);
            win.CancelButton.Visibility = Visibility.Collapsed;

            var owner = FindOwner();
            if (owner != null && owner != win) win.Owner = owner;

            win.ShowDialog();
        }


        public static bool Confirm(string message, string title = "Подтверждение")
        {
            var win = new AppMessageBoxWindow();
            win.TitleBlock.Text = title;
            win.MessageBlock.Text = message;
            win.ApplyIcon(AppMessageBoxIcon.Question);
            win.CancelButton.Visibility = Visibility.Visible;
            win.OkButton.Content = "Да";

            var owner = FindOwner();
            if (owner != null && owner != win) win.Owner = owner;

            win.ShowDialog();
            return win._result;
        }
    }
}
