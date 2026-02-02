using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Velopack;
using Velopack.Sources;
using Xcelerator.ViewModels;

namespace Xcelerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // DataContext is set by App.xaml.cs using dependency injection
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                ((Button)sender).Content = "□";
            }
            else
            {
                WindowState = WindowState.Maximized;
                ((Button)sender).Content = "❐";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create update manager with the deployment path
                var updateManager = new UpdateManager(@"\\SOA-QACTL01\Xcelerator\Remote");

                // Check for updates
                var newVersion = await updateManager.CheckForUpdatesAsync();

                if (newVersion == null)
                {
                    MessageBox.Show(
                        "You are using the latest version",
                        "Check for Updates",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    // Ask user if they want to download and install the update
                    var result = MessageBox.Show(
                        $"A new version {newVersion.TargetFullRelease.Version} is available. Would you like to download and install it?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Download and apply the update
                        await updateManager.DownloadUpdatesAsync(newVersion);

                        MessageBox.Show(
                            "Update has been downloaded and will be applied when you restart the application.",
                            "Update Ready",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Apply updates and restart
                        updateManager.ApplyUpdatesAndRestart(newVersion);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error checking for updates: {ex.Message}",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}