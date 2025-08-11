using System.Windows.Input;

namespace Xcelerator.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private string _selectedModule = string.Empty;

        public DashboardViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            
            GoBackCommand = new RelayCommand(GoBack);
            SelectModuleCommand = new RelayCommand<string>(SelectModule);
        }

        public string SelectedModule
        {
            get => _selectedModule;
            set => SetProperty(ref _selectedModule, value);
        }

        public ICommand GoBackCommand { get; }
        public ICommand SelectModuleCommand { get; }

        public string[] AvailableModules { get; } = new[]
        {
            "ContactOrchestrator",
            "ContactForge", 
            "AgentForge",
            "ConnectGrid",
            "PulseOps"
        };

        private void GoBack()
        {
            _mainViewModel.NavigateBackCommand.Execute(null);
        }

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
