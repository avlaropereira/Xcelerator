# Using Authenticated API Calls in ViewModels

This document explains how to use the authentication token stored in cluster instances to make authenticated API calls from any ViewModel.

## Overview

After successful authentication, each `Cluster` instance stores:
- `AuthToken` - The access token
- `TokenType` - Token type (usually "Bearer")
- `RefreshToken` - Token for refreshing access
- `ResourceServerBaseUri` - Base URL for API calls
- `TokenExpirationTime` - When the token expires

## Quick Start

### 1. Inject the AuthenticatedHttpClientFactory

In your ViewModel constructor, inject `IAuthenticatedHttpClientFactory`:

```csharp
using Xcelerator.Services;
using Xcelerator.Models;

public class MyViewModel : BaseViewModel
{
    private readonly IAuthenticatedHttpClientFactory _httpClientFactory;
    private readonly Cluster _cluster;

    public MyViewModel(IAuthenticatedHttpClientFactory httpClientFactory, Cluster cluster)
    {
        _httpClientFactory = httpClientFactory;
        _cluster = cluster;
    }
}
```

### 2. Make API Calls

Use the factory to create an authenticated HttpClient:

```csharp
private async Task<string> GetDataFromApiAsync()
{
    try
    {
        // Check if the cluster has a valid token
        if (!_httpClientFactory.HasValidToken(_cluster))
        {
            // Handle invalid/expired token
            return "Token is invalid or expired. Please re-authenticate.";
        }

        // Create an authenticated HttpClient
        using var client = _httpClientFactory.CreateClient(_cluster);

        // Make your API call
        var response = await client.GetAsync("/api/your-endpoint");
        
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            return $"API call failed: {response.StatusCode}";
        }
    }
    catch (Exception ex)
    {
        return $"Error: {ex.Message}";
    }
}
```

## Complete Example: DashboardViewModel with API Call

```csharp
using System.Net.Http;
using System.Windows.Input;
using Xcelerator.Models;
using Xcelerator.Services;

namespace Xcelerator.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IAuthenticatedHttpClientFactory _httpClientFactory;
        private readonly Cluster _cluster;
        private string _selectedModule = string.Empty;
        private string _apiData = string.Empty;
        private bool _isLoading = false;

        public DashboardViewModel(
            MainViewModel mainViewModel, 
            IAuthenticatedHttpClientFactory httpClientFactory,
            Cluster cluster)
        {
            _mainViewModel = mainViewModel;
            _httpClientFactory = httpClientFactory;
            _cluster = cluster;
            
            SelectModuleCommand = new RelayCommand<string>(SelectModule);
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
        }

        public string SelectedModule
        {
            get => _selectedModule;
            set => SetProperty(ref _selectedModule, value);
        }

        public string ApiData
        {
            get => _apiData;
            set => SetProperty(ref _apiData, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand SelectModuleCommand { get; }
        public ICommand LoadDataCommand { get; }

        private void SelectModule(string? module)
        {
            if (module != null)
            {
                SelectedModule = module;
            }
        }

        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Check if token is valid
                if (!_httpClientFactory.HasValidToken(_cluster))
                {
                    ApiData = "Authentication token expired. Please sign in again.";
                    return;
                }

                // Create authenticated client
                using var client = _httpClientFactory.CreateClient(_cluster);

                // Make API call
                var response = await client.GetAsync("/api/dashboard/data");
                
                if (response.IsSuccessStatusCode)
                {
                    ApiData = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    ApiData = $"Failed to load data: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                ApiData = $"Error loading data: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
```

## Accessing Current Cluster from PanelViewModel

If you need to pass the current cluster to a ViewModel:

```csharp
// In PanelViewModel.cs
private void NavigateToDashboard()
{
    if (SelectedClusterForLogin != null)
    {
        // Pass the cluster and httpClientFactory to DashboardViewModel
        var httpClientFactory = App.AppHost?.Services.GetRequiredService<IAuthenticatedHttpClientFactory>();
        
        var dashboardViewModel = new DashboardViewModel(
            _mainViewModel, 
            httpClientFactory,
            SelectedClusterForLogin  // Pass the current cluster
        )
        {
            SelectedModule = SelectedClusterForLogin.SelectedModule
        };
        
        CurrentViewModel = dashboardViewModel;
    }
}
```

## Token Properties Available

Each `Cluster` instance provides:

```csharp
// Check if cluster has valid credentials
bool hasCredentials = cluster.HasCredentials;

// Check if cluster has valid token
bool hasValidToken = cluster.HasValidToken;

// Access token properties
string token = cluster.AuthToken;
string tokenType = cluster.TokenType;  // Usually "Bearer"
string baseUri = cluster.ResourceServerBaseUri;
DateTime? expiresAt = cluster.TokenExpirationTime;
string refreshToken = cluster.RefreshToken;
```

## Using with Specific NICE Services

If you're using the AdminService or other NICE services:

```csharp
public class MyViewModel : BaseViewModel
{
    private readonly IAdminService _adminService;
    private readonly Cluster _cluster;

    public MyViewModel(IAdminService adminService, Cluster cluster)
    {
        _adminService = adminService;
        _cluster = cluster;
    }

    private async Task GetSkillsAsync()
    {
        try
        {
            // The AdminService will need the token - you can configure it
            var skills = await _adminService.GetSkillsAsync(_cluster.AuthToken);
            // Process skills...
        }
        catch (Exception ex)
        {
            // Handle error
        }
    }
}
```

## Best Practices

1. **Always check token validity** before making API calls using `HasValidToken()`
2. **Handle token expiration** gracefully and prompt user to re-authenticate
3. **Dispose HttpClient properly** using `using` statement
4. **Catch exceptions** and provide user-friendly error messages
5. **Show loading indicators** during API calls
6. **Store the cluster reference** if you need to make multiple API calls

## Token Lifecycle

1. User enters credentials in `LoginView`
2. `LoginViewModel` calls `AuthService.AuthenticateAsync()`
3. On success, token is saved to `Cluster` instance
4. Token is available to all ViewModels that have access to the cluster
5. Token remains valid until expiration or logout
6. When cluster is deselected, all token data is cleared

## Debugging Tips

- Check `cluster.HasValidToken` to verify token is present and not expired
- Use `System.Diagnostics.Debug.WriteLine()` to log token status
- Verify `cluster.ResourceServerBaseUri` is set correctly
- Check API response status codes for authentication errors (401, 403)
