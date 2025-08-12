using System.Windows.Input;

namespace Xcelerator.ViewModels
{
    /// <summary>
    /// ViewModel for the DashboardView that manages module selection and navigation
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private string _selectedModule = string.Empty;

        public DashboardViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            
            SelectModuleCommand = new RelayCommand<string>(SelectModule);
        }

        /// <summary>
        /// Currently selected module
        /// </summary>
        public string SelectedModule
        {
            get => _selectedModule;
            set => SetProperty(ref _selectedModule, value);
        }

        public ICommand SelectModuleCommand { get; }

        /// <summary>
        /// Available modules for selection
        /// </summary>
        public string[] AvailableModules { get; } = new[]
        {
            "ContactOrchestrator",
            "ContactForge", 
            "AgentForge",
            "ConnectGrid",
            "PulseOps"
        };



        /// <summary>
        /// Select a module
        /// </summary>
        private void SelectModule(string? module)
        {
            if (module != null)
            {
                SelectedModule = module;
                // Here you would typically navigate to the specific module page
                // For now, we'll just update the selected module
            }
        }
    }
}
