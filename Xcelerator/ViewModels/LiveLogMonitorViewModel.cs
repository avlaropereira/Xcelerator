using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Xcelerator.Models;
using Xcelerator.Services;

namespace Xcelerator.ViewModels
{
    /// <summary>
    /// ViewModel for the LiveLogMonitorView that manages log monitoring functionality
    /// </summary>
    public class LiveLogMonitorViewModel : BaseViewModel
    {
        private readonly Cluster? _cluster;
        private readonly LogFileManager _logFileManager;
        private Dictionary<string, string>? _tokenData;
        private string _status = "Ready";
        private string _searchText = string.Empty;
        private bool _isRegexMode = false;
        private bool _isCaseSensitive = false;
        private int _matchCount = 0;
        private ObservableCollection<RemoteMachineItem> _remoteMachines;
        private RemoteMachineItem? _selectedRemoteMachine;
        private ObservableCollection<ITabViewModel> _openTabs;
        private ObservableCollection<LogSearchResultGroup> _searchResultGroups;
        private LogSearchResult? _selectedSearchResult;
        private bool _isSearching;
        private int _selectedTabIndex;
        private CancellationTokenSource? _searchCancellation;

        public ICommand OpenMachineTabCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand NavigateToSearchResultCommand { get; }
        public ICommand SearchCommand { get; }

        public LiveLogMonitorViewModel(MainViewModel mainViewModel, DashboardViewModel dashboardViewModel, LogFileManager logFileManager, Cluster? cluster = null, Dictionary<string, string>? tokenData = null)
        {
            _cluster = cluster;
            _tokenData = tokenData;
            _logFileManager = logFileManager ?? throw new ArgumentNullException(nameof(logFileManager));

            // Initialize open tabs collection
            _openTabs = new ObservableCollection<ITabViewModel>();
            
            // Initialize search result groups collection (hierarchical by tab)
            _searchResultGroups = new ObservableCollection<LogSearchResultGroup>();

            // Initialize remote machines dynamically based on cluster name
            _remoteMachines = new ObservableCollection<RemoteMachineItem>();
            InitializeRemoteMachines();

            // Initialize OpenMachineTabCommand
            OpenMachineTabCommand = new RelayCommand<RemoteMachineItem>(ExecuteOpenMachineTab, CanExecuteOpenMachineTab);
            
            // Initialize CloseTabCommand
            CloseTabCommand = new RelayCommand<ITabViewModel>(ExecuteCloseTab, CanExecuteCloseTab);
            
            // Initialize NavigateToSearchResultCommand
            NavigateToSearchResultCommand = new RelayCommand<LogSearchResult>(ExecuteNavigateToSearchResult, CanExecuteNavigateToSearchResult);
            
            // Initialize SearchCommand (triggered by Enter key)
            SearchCommand = new RelayCommand(ExecuteSearch, CanExecuteSearch);

            // Set initial status
            Status = "Ready";
        }

        #region Properties

        /// <summary>
        /// User name from token
        /// </summary>
        public string UserName
        {
            get
            {
                if (_tokenData == null) return "User";
                if (_tokenData.TryGetValue("name", out var name)) return name;
                if (_tokenData.TryGetValue("given_name", out var givenName)) return givenName;
                return "User";
            }
        }

        /// <summary>
        /// Agent ID from token
        /// </summary>
        public string IcAgentId
        {
            get
            {
                if (_tokenData == null) return "N/A";
                if (_tokenData.TryGetValue("icAgentId", out var agentId)) return agentId;
                return "N/A";
            }
        }

        /// <summary>
        /// Cluster ID from token
        /// </summary>
        public string IcClusterId
        {
            get
            {
                if (_tokenData == null) return "N/A";
                if (_tokenData.TryGetValue("icClusterId", out var clusterId)) return clusterId;
                return "N/A";
            }
        }

        /// <summary>
        /// Cluster name
        /// </summary>
        public string ClusterName => _cluster?.DisplayName ?? "Unknown";

        /// <summary>
        /// Current monitoring status
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// Search text for filtering machines
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value); // Remove automatic search
        }

        /// <summary>
        /// Whether regex mode is enabled for search
        /// </summary>
        public bool IsRegexMode
        {
            get => _isRegexMode;
            set => SetProperty(ref _isRegexMode, value);
        }
        
        /// <summary>
        /// Whether case sensitive search is enabled
        /// </summary>
        public bool IsCaseSensitive
        {
            get => _isCaseSensitive;
            set => SetProperty(ref _isCaseSensitive, value);
        }

        /// <summary>
        /// Number of search results matching the filter
        /// </summary>
        public int MatchCount
        {
            get => _matchCount;
            private set => SetProperty(ref _matchCount, value);
        }
        
        /// <summary>
        /// Collection of grouped search results from all open tabs (hierarchical by tab)
        /// </summary>
        public ObservableCollection<LogSearchResultGroup> SearchResultGroups
        {
            get => _searchResultGroups;
            private set => SetProperty(ref _searchResultGroups, value);
        }
        
        /// <summary>
        /// Currently selected search result
        /// </summary>
        public LogSearchResult? SelectedSearchResult
        {
            get => _selectedSearchResult;
            set => SetProperty(ref _selectedSearchResult, value);
        }
        
        /// <summary>
        /// Indicates whether a search operation is in progress
        /// </summary>
        public bool IsSearching
        {
            get => _isSearching;
            private set => SetProperty(ref _isSearching, value);
        }
        
        /// <summary>
        /// Selected tab index for programmatic tab selection
        /// </summary>
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        /// <summary>
        /// Collection of remote machines available for log download
        /// </summary>
        public ObservableCollection<RemoteMachineItem> RemoteMachines
        {
            get => _remoteMachines;
            private set => SetProperty(ref _remoteMachines, value);
        }

        /// <summary>
        /// Currently selected remote machine for log download
        /// </summary>
        public RemoteMachineItem? SelectedRemoteMachine
        {
            get => _selectedRemoteMachine;
            set => SetProperty(ref _selectedRemoteMachine, value);
        }

        /// <summary>
        /// Collection of open log tabs
        /// </summary>
        public ObservableCollection<ITabViewModel> OpenTabs
        {
            get => _openTabs;
            set => SetProperty(ref _openTabs, value);
        }

        #endregion

        #region Remote Machines Initialization

        /// <summary>
        /// Initialize remote machines dynamically from cluster topology
        /// </summary>
        private void InitializeRemoteMachines()
        {
            // Ensure cluster exists and has topology data
            if (_cluster == null || _cluster.Topology == null)
            {
                System.Diagnostics.Debug.WriteLine("Cluster or topology is null, no remote machines will be loaded");
                return;
            }

            // Check if topology has servers
            if (_cluster.Topology.Servers == null || _cluster.Topology.Servers.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"No servers found in topology for cluster '{_cluster.Name}'");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Loading {_cluster.Topology.Servers.Count} servers from topology for cluster '{_cluster.Name}'");

            // Iterate through all servers in the topology
            foreach (var server in _cluster.Topology.Servers)
            {
                // Create parent machine item for the server
                var serverItem = new RemoteMachineItem
                {
                    Name = server.Name,
                    DisplayName = server.Name,
                    IsExpanded = false
                };

                // Add all services as children
                if (server.Services != null && server.Services.Count > 0)
                {
                    foreach (var service in server.Services)
                    {
                        // Create a unique identifier for the service
                        // Format: ServerName-ServiceInternalName
                        var serviceIdentifier = string.IsNullOrEmpty(service.InternalName) 
                            ? $"{server.Name}-{service.DisplayName.Replace(" ", "")}"
                            : $"{server.Name}-{service.InternalName}";

                        var serviceItem = new RemoteMachineItem
                        {
                            Name = serviceIdentifier,
                            DisplayName = service.DisplayName
                        };

                        serverItem.Children.Add(serviceItem);
                    }

                    System.Diagnostics.Debug.WriteLine($"  Server '{server.Name}': {server.Services.Count} services loaded");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  Server '{server.Name}': No services found");
                }

                // Add the server to the remote machines collection
                _remoteMachines.Add(serverItem);
            }

            System.Diagnostics.Debug.WriteLine($"Successfully loaded {_remoteMachines.Count} servers with topology data");
        }

        #endregion

        #region Log Search

        /// <summary>
        /// Searches through all open log tabs for matching entries with parallel processing and cancellation support
        /// </summary>
        private async Task SearchLogsAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                // Cancel any ongoing search
                _searchCancellation?.Cancel();
                _searchCancellation = null;
                
                // Clear results if search is empty
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SearchResultGroups.Clear();
                    MatchCount = 0;
                    Status = "Ready";
                });
                return;
            }

            // Cancel previous search if still running
            _searchCancellation?.Cancel();
            _searchCancellation = new CancellationTokenSource();
            var cancellationToken = _searchCancellation.Token;

            IsSearching = true;
            Status = "Searching...";

            try
            {
                // Clear previous results immediately
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SearchResultGroups.Clear();
                    MatchCount = 0;
                });

                // Perform search on background thread with parallel processing
                var resultGroups = await Task.Run(() => PerformLogSearchParallel(searchText, cancellationToken), cancellationToken);

                // Check if cancelled before updating UI
                if (!cancellationToken.IsCancellationRequested)
                {
                    // Calculate total matches on background thread
                    int totalMatches = resultGroups.Sum(g => g.ResultCount);
                    bool hasLimitedResults = resultGroups.Any(g => g.ResultCount >= 5000);
                    
                    // Update UI on UI thread with batched operations
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // Use batched add to minimize UI updates
                        foreach (var group in resultGroups)
                        {
                            SearchResultGroups.Add(group);
                        }
                        MatchCount = totalMatches;
                        
                        if (hasLimitedResults)
                        {
                            Status = $"Found {MatchCount:N0}+ matches across {resultGroups.Count} tab(s) (some tabs limited to 5000 results)";
                        }
                        else
                        {
                            Status = $"Found {MatchCount:N0} matches across {resultGroups.Count} tab(s)";
                        }
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled, this is expected
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Status = "Search cancelled";
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Status = $"Search error: {ex.Message}";
                    SearchResultGroups.Clear();
                    MatchCount = 0;
                });
            }
            finally
            {
                IsSearching = false;
            }
        }

        /// <summary>
        /// Performs parallel log search across all tabs and groups results by tab
        /// </summary>
        private List<LogSearchResultGroup> PerformLogSearchParallel(string searchText, CancellationToken cancellationToken)
        {
            var tabs = OpenTabs.OfType<LogTabViewModel>().ToList();
            
            // Use Parallel.ForEach for concurrent processing of tabs
            var groupsBag = new System.Collections.Concurrent.ConcurrentBag<LogSearchResultGroup>();
            
            Parallel.ForEach(tabs, new ParallelOptions 
            { 
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            }, tab =>
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                var tabResults = SearchInTabOptimized(tab, searchText, cancellationToken);
                
                // Only create a group if there are results
                if (tabResults.Count > 0)
                {
                    var group = new LogSearchResultGroup
                    {
                        TabName = tab.HeaderName,
                        IsExpanded = true
                    };
                    
                    // Add results already sorted by line number
                    foreach (var result in tabResults)
                    {
                        group.Results.Add(result);
                    }
                    
                    groupsBag.Add(group);
                }
            });

            // Sort groups by tab name and return
            return groupsBag.OrderBy(g => g.TabName).ToList();
        }

        /// <summary>
        /// Optimized search for matches within a single tab with cancellation support and compiled regex
        /// </summary>
        private List<LogSearchResult> SearchInTabOptimized(LogTabViewModel tab, string searchText, CancellationToken cancellationToken)
        {
            var results = new List<LogSearchResult>();
            
            if (tab.LogLines == null || tab.LogLines.Count == 0)
                return results;

            // Limit results per tab to prevent UI overload
            const int maxResultsPerTab = 5000;

            // Pre-compile regex if in regex mode for better performance
            Regex? regex = null;
            if (IsRegexMode)
            {
                try
                {
                    var regexOptions = IsCaseSensitive ? RegexOptions.Compiled : RegexOptions.Compiled | RegexOptions.IgnoreCase;
                    regex = new Regex(searchText, regexOptions);
                }
                catch (ArgumentException)
                {
                    // Invalid regex, return empty results
                    return results;
                }
            }
            
            var comparison = IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int lineNumber = 0;
            const int batchCheckSize = 1000; // Check for cancellation every 1000 lines
            
            foreach (var logEntry in tab.LogLines)
            {
                lineNumber++;
                
                // Check for cancellation periodically to avoid overhead
                if (lineNumber % batchCheckSize == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                
                bool isMatch = false;
                
                if (regex != null)
                {
                    isMatch = regex.IsMatch(logEntry);
                }
                else
                {
                    isMatch = logEntry.Contains(searchText, comparison);
                }

                if (isMatch)
                {
                    results.Add(new LogSearchResult
                    {
                        TabName = tab.HeaderName,
                        LogEntry = logEntry,
                        LineNumber = lineNumber,
                        Preview = CreatePreview(logEntry, 100),
                        SourceTab = tab
                    });
                    
                    // Stop if we've hit the limit to prevent UI freezing
                    if (results.Count >= maxResultsPerTab)
                    {
                        break;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Creates a shortened preview of a log entry
        /// </summary>
        private string CreatePreview(string logEntry, int maxLength)
        {
            if (string.IsNullOrEmpty(logEntry))
                return string.Empty;

            // Remove newlines and extra whitespace for preview
            var preview = Regex.Replace(logEntry, @"\s+", " ").Trim();
            
            if (preview.Length <= maxLength)
                return preview;

            return preview.Substring(0, maxLength) + "...";
        }

        #endregion

        #region Commands

        /// <summary>
        /// Determines whether a new tab can be opened for the specified remote machine
        /// </summary>
        private bool CanExecuteOpenMachineTab(RemoteMachineItem? remoteMachine)
        {
            return remoteMachine != null && remoteMachine.IsLeaf;
        }

        /// <summary>
        /// Opens a new tab for the specified remote machine
        /// </summary>
        private void ExecuteOpenMachineTab(RemoteMachineItem? remoteMachine)
        {
            if (remoteMachine == null || !remoteMachine.IsLeaf)
                return;

            // Check if a tab for this machine already exists
            var existingTab = OpenTabs
                .OfType<LogTabViewModel>()
                .FirstOrDefault(tab => tab.RemoteMachine?.Name == remoteMachine.Name);

            if (existingTab != null)
            {
                // Tab already exists, just select it (WPF will handle the selection automatically)
                return;
            }

            // Extract server name and service information from topology
            string? serverName = null;
            string? machineItemName = null;

            if (_cluster?.Topology?.Servers != null)
            {
                // Parse the remoteMachine.Name to extract server name
                // Format is: {ServerName}-{ServiceInternalName}
                var nameSegments = remoteMachine.Name.Split('-');

                // Server name is typically the first 2 segments (e.g., "SOA-C30COR01")
                if (nameSegments.Length >= 3)
                {
                    var potentialServerName = string.Join("-", nameSegments.Take(2));

                    // Find matching server in topology
                    var matchingServer = _cluster.Topology.Servers
                        .FirstOrDefault(s => s.Name.Equals(potentialServerName, StringComparison.OrdinalIgnoreCase));

                    if (matchingServer != null)
                    {
                        serverName = matchingServer.Name;

                        // Find matching service by display name
                        var matchingService = matchingServer.Services
                            .FirstOrDefault(svc => svc.DisplayName.Equals(remoteMachine.DisplayName, StringComparison.OrdinalIgnoreCase));

                        if (matchingService != null)
                        {
                            machineItemName = matchingService.InternalName;

                            System.Diagnostics.Debug.WriteLine(
                                $"Topology match found - Server: '{serverName}', Service: '{machineItemName}'"
                            );
                        }
                    }
                }
            }

            // Fallback to remoteMachine properties if topology lookup fails
            if (string.IsNullOrEmpty(serverName) || string.IsNullOrEmpty(machineItemName))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Topology lookup failed for '{remoteMachine.Name}', using fallback values"
                );

              
            }

            // Create a new LogTabViewModel with server name and machine item name
            var logTab = new LogTabViewModel(
                remoteMachine, 
                _logFileManager, 
                serverName,
                machineItemName)
            {
                RemoteMachine = remoteMachine
            };

            System.Diagnostics.Debug.WriteLine(
                $"Created log tab for Machine: '{remoteMachine.Name}', Server: '{serverName}', Service: '{machineItemName}'"
            );

            // Add the new tab to the OpenTabs collection
            OpenTabs.Add(logTab);
        }

        /// <summary>
        /// Determines whether a tab can be closed
        /// </summary>
        private bool CanExecuteCloseTab(ITabViewModel? tab)
        {
            return tab != null && OpenTabs.Contains(tab);
        }

        /// <summary>
        /// Closes the specified tab and cleans up its resources
        /// </summary>
        private void ExecuteCloseTab(ITabViewModel? tab)
        {
            if (tab == null)
                return;

            // If the tab is a LogTabViewModel, call its Cleanup method
            if (tab is LogTabViewModel logTab)
            {
                logTab.Cleanup();
            }

            // Remove the tab from the collection
            OpenTabs.Remove(tab);
        }
        
        /// <summary>
        /// Determines whether navigation to search result is possible
        /// </summary>
        private bool CanExecuteNavigateToSearchResult(LogSearchResult? result)
        {
            return result != null && result.SourceTab is LogTabViewModel;
        }

        /// <summary>
        /// Navigates to the selected search result
        /// </summary>
        private void ExecuteNavigateToSearchResult(LogSearchResult? result)
        {
            if (result == null || result.SourceTab is not LogTabViewModel tab)
                return;

            // Set the selected log line in the tab to show it in the detail panel
            tab.SelectedLogLine = result.LogEntry;
            tab.IsDetailPanelVisible = true;
            
            // Find the tab in the OpenTabs collection and make it the active tab
            var tabIndex = OpenTabs.IndexOf(tab);
            if (tabIndex >= 0)
            {
                // We need to trigger the TabControl to select this tab
                // This will be handled through the SelectedTabIndex property
                SelectedTabIndex = tabIndex;
            }
        }
        
        /// <summary>
        /// Determines whether search can be executed
        /// </summary>
        private bool CanExecuteSearch()
        {
            return !string.IsNullOrWhiteSpace(SearchText) && !IsSearching;
        }
        
        /// <summary>
        /// Executes the search (triggered by Enter key)
        /// </summary>
        private void ExecuteSearch()
        {
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                _ = SearchLogsAsync(SearchText);
            }
        }

        #endregion
    }
}



