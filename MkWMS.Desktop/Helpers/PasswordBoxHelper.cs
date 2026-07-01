using System.Windows;
using System.Windows.Controls;

namespace MkWMS.Desktop.Helpers
{
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BoundPassword",
                typeof(string),
                typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

        public static readonly DependencyProperty BindPasswordProperty =
            DependencyProperty.RegisterAttached(
                "BindPassword",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnBindPasswordChanged));

        private static readonly DependencyProperty UpdatingPasswordProperty =
            DependencyProperty.RegisterAttached(
                "UpdatingPassword",
                typeof(bool),
                typeof(PasswordBoxHelper),
                new PropertyMetadata(false));

        public static string GetBoundPassword(DependencyObject obj) =>
            (string)obj.GetValue(BoundPasswordProperty);

        public static void SetBoundPassword(DependencyObject obj, string value) =>
            obj.SetValue(BoundPasswordProperty, value);

        public static bool GetBindPassword(DependencyObject obj) =>
            (bool)obj.GetValue(BindPasswordProperty);

        public static void SetBindPassword(DependencyObject obj, bool value) =>
            obj.SetValue(BindPasswordProperty, value);

        private static bool GetUpdatingPassword(DependencyObject obj) =>
            (bool)obj.GetValue(UpdatingPasswordProperty);

        private static void SetUpdatingPassword(DependencyObject obj, bool value) =>
            obj.SetValue(UpdatingPasswordProperty, value);

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox passwordBox)
                return;

            passwordBox.PasswordChanged -= PasswordChanged;

            if (!GetUpdatingPassword(passwordBox))
            {
                passwordBox.Password = e.NewValue as string ?? string.Empty;
            }

            passwordBox.PasswordChanged += PasswordChanged;
        }

        private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox passwordBox)
                return;

            bool wasBound = (bool)e.OldValue;
            bool shouldBind = (bool)e.NewValue;

            if (wasBound)
                passwordBox.PasswordChanged -= PasswordChanged;

            if (shouldBind)
                passwordBox.PasswordChanged += PasswordChanged;
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                SetUpdatingPassword(passwordBox, true);
                SetBoundPassword(passwordBox, passwordBox.Password);
                SetUpdatingPassword(passwordBox, false);
            }
        }
    }
}