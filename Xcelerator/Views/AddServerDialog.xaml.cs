using System.Windows;
using Xcelerator.Services;

namespace Xcelerator.Views
{
    /// <summary>
    /// Interaction logic for AddServerDialog.xaml
    /// </summary>
    public partial class AddServerDialog : Window
    {
        public string ServerName { get; private set; } = string.Empty;
        public bool ServerAdded { get; private set; } = false;

        public AddServerDialog(string clusterName)
        {
            InitializeComponent();

            // Set the title with the actual cluster name
            Title = $"Add Server to {clusterName}";

            // Focus on the text box when the dialog opens
            Loaded += (s, e) => ServerNameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ServerName = ServerNameTextBox.Text.Trim().ToUpper();

            if (string.IsNullOrEmpty(ServerName))
            {
                MessageBox.Show("Please enter a server name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate server name format
            var (clusterCode, serverType, isValid) = ServerConfigManager.ParseServerName(ServerName);

            if (!isValid)
            {
                MessageBox.Show(
                    $"Invalid server name format: {ServerName}\n\n" +
                    "Expected format: XXX-CY[Y]SSS##\n" +
                    "Where SSS must be a valid server type: COR, API, WEB, MED, IVR, AGM, or AGT\n\n" +
                    "Examples:\n" +
                    "  • TCB-C1COR01 (Cluster 1, Core server)\n" +
                    "  • TCA-C34COR01 (Cluster 34, Core server)\n" +
                    "  • TOA-C32API01 (Cluster 32, API server)\n" +
                    "  • SOA-C30WEB01 (Cluster 30, Web server)\n" +
                    "  • TCA-C5MED01 (Cluster 5, Media server)",
                    "Invalid Server Name",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Show confirmation dialog with details
            string clusterName = ServerConfigManager.MapClusterCodeToName(ServerName, clusterCode);
            string logPath = ServerConfigManager.GetLogDirectoryPath(ServerName);
            bool clusterExists = ServerConfigManager.ClusterExists(clusterName);

            string confirmationMessage = $"Server Details:\n\n" +
                $"Name: {ServerName}\n" +
                $"Cluster: {clusterName}";

            if (!clusterExists)
            {
                confirmationMessage += " (NEW - will be created)";
            }

            confirmationMessage += $"\nType: {serverType}\n" +
                $"Log Path: {logPath}\n\n" +
                "Add this server to the configuration?";

            var result = MessageBox.Show(
                confirmationMessage,
                "Confirm Server Addition",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            // Disable the OK button while processing
            OkButton.IsEnabled = false;
            OkButton.Content = "Adding...";

            // Add server to configuration
            if (ServerConfigManager.AddServerToCluster(ServerName, out string errorMessage))
            {
                ServerAdded = true;

                string successMessage = $"Server '{ServerName}' has been successfully added to cluster '{clusterName}'.";

                if (!clusterExists)
                {
                    successMessage += $"\n\nNew cluster '{clusterName}' was created.";
                }

                successMessage += "\n\nThe topology is being reloaded automatically to show the new server.";

                MessageBox.Show(
                    successMessage,
                    "Server Added Successfully",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(
                    $"Failed to add server:\n\n{errorMessage}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Re-enable the button
                OkButton.IsEnabled = true;
                OkButton.Content = "OK";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
