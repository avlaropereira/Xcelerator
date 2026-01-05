using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Xcelerator.Models;

namespace Xcelerator.ViewModels
{
    /// <summary>
    /// ViewModel for the LiveLogMonitorView that manages log monitoring functionality
    /// </summary>
    public class LiveLogMonitorViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly DashboardViewModel _dashboardViewModel;
        private readonly Cluster? _cluster;
        private Dictionary<string, string>? _tokenData;
        private string _logContent = string.Empty;
        private string _status = "Ready";
        private bool _hasLogs = false;
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

        public LiveLogMonitorViewModel(MainViewModel mainViewModel, DashboardViewModel dashboardViewModel, Cluster? cluster = null, Dictionary<string, string>? tokenData = null)
        {
            _mainViewModel = mainViewModel;
            _dashboardViewModel = dashboardViewModel;
            _cluster = cluster;
            _tokenData = tokenData;

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
        /// Decoded token data
        /// </summary>
        public Dictionary<string, string>? TokenData
        {
            get => _tokenData;
            private set => SetProperty(ref _tokenData, value);
        }

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
        /// Log content to display
        /// </summary>
        public string LogContent
        {
            get => _logContent;
            set => SetProperty(ref _logContent, value);
        }

        /// <summary>
        /// Current monitoring status
        /// </summary>
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// Whether there are logs to display
        /// </summary>
        public bool HasLogs
        {
            get => _hasLogs;
            set => SetProperty(ref _hasLogs, value);
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
        /// Initialize remote machines based on cluster name
        /// </summary>
        private void InitializeRemoteMachines()
        {
            // Ensure cluster exists and has a valid name
            if (_cluster == null || string.IsNullOrEmpty(_cluster.Name))
            {
                return;
            }

            // Extract letters and last digit from cluster name
            var clusterName = _cluster.Name;
            var match = Regex.Match(clusterName, @"^([A-Za-z]+)(\d+)$");
            
            if (!match.Success)
            {
                return;
            }

            var letters = match.Groups[1].Value;
            var numbers = match.Groups[2].Value;
            
            // Generate machine names in format: {letters}-C{lastDigit}{type}
            // Example: SC10 -> SC-C0COR01, SC-C0API01, SC-C0WEB01, SC-C0MED01
            
            // COR01 - Virtual Cluster with sub-services
            var cor01 = new RemoteMachineItem
            {
                Name = $"{letters}A-C{numbers}COR01",
                DisplayName = $"{letters}-C{numbers}COR01",
                IsExpanded = false
            };
            
            // Add COR01 Virtual Cluster services as children
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}COR01-VirtualCluster", DisplayName = "Virtual Cluster" });
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}COR01-FileServer", DisplayName = "File Server" });
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}COR01-CoOpService", DisplayName = "CoOp Service" });
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}COR01-SurvyService", DisplayName = "Survy Service" });
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}COR01-FSDrivePublisher", DisplayName = "FS Drive Publisher" });
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}COR01-DroneLetter", DisplayName = "Drone Letter" });
            cor01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}COR01-DBCWS", DisplayName = "DBCWS" });
            
            _remoteMachines.Add(cor01);
            
            // API01 - API Services with sub-services
            var api01 = new RemoteMachineItem
            {
                Name = $"{letters}A-C{numbers}API01",
                DisplayName = $"{letters}-C{numbers}API01",
                IsExpanded = false
            };
            
            // Add API01 API Services as children
            api01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}API01-L7Healthcheck", DisplayName = "L7 Healthcheck" });
            api01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}API01-DroneService", DisplayName = "Drone Service" });
            api01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}API01-APIWebsite", DisplayName = "API Website" });
            api01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}API01-AutoSite", DisplayName = "AutoSite" });
            api01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}API01-DBCWS", DisplayName = "DBCWS" });
            
            _remoteMachines.Add(api01);
            
            // WEB01 - Web Services with sub-services
            var web01 = new RemoteMachineItem
            {
                Name = $"{letters}A-C{numbers}WEB01",
                DisplayName = $"{letters}-C{numbers}WEB01",
                IsExpanded = false
            };
            
            // Add WEB01 Web Services as children
            web01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}WEB01-Agent", DisplayName = "Agent" });
            web01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}WEB01-AuthenticationServer", DisplayName = "Authentication Server" });
            web01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}WEB01-CacheSite", DisplayName = "Cache Site" });
            web01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}WEB01-inContact", DisplayName = "inContact" });
            web01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}WEB01-inControl", DisplayName = "inControl" });
            web01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}WEB01-ReportService", DisplayName = "Report Service" });
            web01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}WEB01-Security", DisplayName = "Security" });
            web01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}WEB01-WebScripting", DisplayName = "WebScripting" });
            web01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}WEB01-DBCWS", DisplayName = "DBCWS" });
            
            _remoteMachines.Add(web01);
            
            // MED01 - Media Services with sub-services
            var med01 = new RemoteMachineItem
            {
                Name = $"{letters}A-C{numbers}MED01",
                DisplayName = $"{letters}-C{numbers}MED01",
                IsExpanded = false
            };
            
            // Add MED01 Media Services as children
            med01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}MED01-MediaServer", DisplayName = "Media Server" });
            med01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}MED01-DroneService", DisplayName = "Drone Service" });
            med01.Children.Add(new RemoteMachineItem { Name = $"{letters}A-C{numbers}MED01-DBCWS", DisplayName = "DBCWS" });
            
            _remoteMachines.Add(med01);
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
        /// Performs the actual log search across all tabs (legacy sequential method kept for compatibility)
        /// </summary>
        private List<LogSearchResult> PerformLogSearch(string searchText)
        {
            var results = new List<LogSearchResult>();
            
            // Search through each open tab
            foreach (var tab in OpenTabs.OfType<LogTabViewModel>())
            {
                var tabResults = SearchInTab(tab, searchText);
                results.AddRange(tabResults);
            }

            // Sort results by tab name, then line number
            return results.OrderBy(r => r.TabName).ThenBy(r => r.LineNumber).ToList();
        }

        /// <summary>
        /// Searches for matches within a single tab
        /// </summary>
        private List<LogSearchResult> SearchInTab(LogTabViewModel tab, string searchText)
        {
            var results = new List<LogSearchResult>();
            
            if (tab.LogLines == null || tab.LogLines.Count == 0)
                return results;

            int lineNumber = 0;
            
            foreach (var logEntry in tab.LogLines)
            {
                lineNumber++;
                
                bool isMatch = false;
                
                if (IsRegexMode)
                {
                    try
                    {
                        var regexOptions = IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                        isMatch = Regex.IsMatch(logEntry, searchText, regexOptions);
                    }
                    catch (ArgumentException)
                    {
                        // Invalid regex, skip this pattern
                        continue;
                    }
                }
                else
                {
                    // Contains match with case sensitivity option
                    var comparison = IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
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
                }
            }

            return results;
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

            // Create a new LogTabViewModel with the remote machine's display name and cluster name
            var logTab = new LogTabViewModel(remoteMachine, _cluster?.Name)
            {
                RemoteMachine = remoteMachine
            };

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



