using System.Windows;

namespace Xcelerator.Views
{
    /// <summary>
    /// Interaction logic for AddServerDialog.xaml
    /// </summary>
    public partial class AddServerDialog : Window
    {
        public string ServerName { get; private set; } = string.Empty;

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
            ServerName = ServerNameTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(ServerName))
            {
                MessageBox.Show("Please enter a server name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
