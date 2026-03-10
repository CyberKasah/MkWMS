using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Controls;

namespace MkWMS.Desktop.Helpers
{
    public static class SafeXamlLoader
    {
        public static T LoadWindow<T>(string xamlPath) where T : Window
        {
            if (!File.Exists(xamlPath))
                throw new FileNotFoundException($"XAML файл не найден: {xamlPath}");

            try
            {
                using var stream = File.OpenRead(xamlPath);
                var window = (T)XamlReader.Load(stream);
                return window;
            }
            catch (XamlParseException ex)
            {
                MessageBox.Show($"Ошибка XAML в файле {xamlPath}:\n{ex.Message}\nСтрока: {ex.LineNumber}, позиция: {ex.LinePosition}", "XAML Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке окна {xamlPath}:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public static T LoadControl<T>(string xamlPath) where T : UserControl
        {
            if (!File.Exists(xamlPath))
                throw new FileNotFoundException($"XAML файл не найден: {xamlPath}");

            try
            {
                using var stream = File.OpenRead(xamlPath);
                var control = (T)XamlReader.Load(stream);
                return control;
            }
            catch (XamlParseException ex)
            {
                MessageBox.Show($"Ошибка XAML в файле {xamlPath}:\n{ex.Message}\nСтрока: {ex.LineNumber}, позиция: {ex.LinePosition}", "XAML Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке контрола {xamlPath}:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }
    }
}