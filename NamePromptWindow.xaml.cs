/*
 * File: NamePromptWindow.xaml.cs
 * Purpose: Simple modal dialog used at startup to capture a display name from the user.
 *
 * Behavior:
 * - Returns the entered name via the UserName property when DialogResult is true.
 * - The dialog is intentionally lightweight and synchronous; it is safe to call
 *   ShowDialog from the UI thread during application startup after audio finishes.
 */
using System.Windows;

namespace CyberAware
{
    public partial class NamePromptWindow : Window
    {
        public string UserName { get; private set; } = string.Empty;

        public NamePromptWindow()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            UserName = NameBox.Text?.Trim() ?? string.Empty;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
