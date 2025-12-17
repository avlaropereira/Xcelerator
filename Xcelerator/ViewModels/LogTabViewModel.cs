using Xcelerator.Models;

namespace Xcelerator.ViewModels
{
    /// <summary>
    /// ViewModel for a log tab that displays logs for a specific remote machine
    /// </summary>
    public class LogTabViewModel : BaseViewModel, ITabViewModel
    {
        private string _headerName;
        private RemoteMachineItem? _remoteMachine;
        private string _logContent = string.Empty;

        /// <summary>
        /// Initializes a new instance of the LogTabViewModel class
        /// </summary>
        /// <param name="name">The name to display in the tab header</param>
        public LogTabViewModel(string name)
        {
            _headerName = name;
        }

        /// <summary>
        /// The display name shown in the tab header
        /// </summary>
        public string HeaderName
        {
            get => _headerName;
            set => SetProperty(ref _headerName, value);
        }

        /// <summary>
        /// The remote machine item associated with this log tab
        /// </summary>
        public RemoteMachineItem? RemoteMachine
        {
            get => _remoteMachine;
            set => SetProperty(ref _remoteMachine, value);
        }

        /// <summary>
        /// The log content to display in this tab
        /// </summary>
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }
    }
}
