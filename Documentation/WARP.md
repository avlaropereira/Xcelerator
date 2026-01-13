# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Project Overview

Xcelerator is a WPF desktop application built with .NET 8 that provides a multi-cluster management interface with OAuth2 token-based authentication and modular dashboard navigation. The application follows MVVM architecture with dependency injection, uses Material Design for styling, and includes a separate API client library (Xcelerator.NiceClient) for NICE platform integration.

## Development Commands

### Build and Run
```powershell
# Build the solution
dotnet build

# Run the application
dotnet run --project Xcelerator\Xcelerator.csproj

# Build in Release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

### Solution Management
```powershell
# Restore NuGet packages
dotnet restore

# Open solution in Visual Studio
start Xcelerator.sln
```

## Architecture

### Solution Structure

The solution consists of two projects:
- **Xcelerator** - Main WPF application with UI and ViewModels
- **Xcelerator.NiceClient** - Reusable API client library for NICE platform services

### Dependency Injection and Hosting

The application uses `Microsoft.Extensions.Hosting` for dependency injection:
- `App.xaml.cs` configures the DI container in the constructor
- `IHost AppHost` provides access to registered services
- ViewModels and services are registered and resolved via DI
- HttpClient is configured with typed clients for API services

**Service Registration (App.xaml.cs):**
```csharp
builder.Services.AddHttpClient<IAuthService, AuthService>();
builder.Services.AddSingleton<MainViewModel>();
builder.Services.AddTransient<DashboardViewModel>();
builder.Services.AddTransient<LoginViewModel>();
builder.Services.AddTransient<PanelViewModel>();
```

### MVVM Pattern Implementation

The application uses a strict MVVM (Model-View-ViewModel) pattern with the following structure:

**Navigation Flow:**
- `MainViewModel` serves as the root ViewModel and owns the `LoginCredentials` model
- `MainViewModel.CurrentPage` property determines the top-level page (currently always `PanelViewModel`)
- `PanelViewModel` manages cluster selection and contains a `CurrentViewModel` property that switches between `LoginViewModel` and `DashboardViewModel` based on cluster authentication state
- Views are automatically mapped to ViewModels using DataTemplates defined in App.xaml

**Key Navigation Pattern:**
1. User selects clusters in `PanelViewModel`
2. Clicking a cluster tag triggers `TagClick` → shows `LoginViewModel` or `DashboardViewModel` depending on whether the cluster has a valid auth token
3. `LoginViewModel` calls `IAuthService.AuthenticateAsync()` asynchronously
4. On successful authentication, token is stored in the cluster and `LoginViewModel` calls `PanelViewModel.OnLoginCompleted()` which switches to `DashboardViewModel`
5. Each cluster independently stores its own credentials, auth token, selected module, and dashboard state

### Authentication and Token Management

**OAuth2 Token-Based Authentication:**
- Uses `IAuthService` (from Xcelerator.NiceClient) for authentication
- Authenticates with NICE platform using OAuth2 password grant flow
- Returns `AuthToken` with `access_token`, `token_type`, `expires_in`, `refresh_token`, and `resource_server_base_uri`
- Tokens are stored per-cluster and validated before API calls

**Token Storage:**
- Each `Cluster` stores: `AuthToken`, `TokenType`, `RefreshToken`, `ResourceServerBaseUri`, `TokenExpirationTime`
- `Cluster.HasValidToken` checks if token exists and hasn't expired
- Token expiration calculated as `DateTime.UtcNow + expires_in seconds`

**Authentication Flow:**
1. User enters credentials in `LoginView`
2. `LoginViewModel.SignIn()` calls `IAuthService.AuthenticateAsync()` asynchronously
3. On success: token is stored in `Cluster`, credentials saved, navigation to dashboard
4. On failure: credentials and any token data cleared, error message displayed
5. Token persists across cluster switches until expiration or deselection

### State Management

**Cluster State:**
- Each `Cluster` model stores its own authentication credentials (`AccessKey`, `SecretKey`)
- Each cluster stores its own OAuth2 token information independently
- `Cluster.HasCredentials` property determines if the cluster has credentials
- `Cluster.HasValidToken` checks for valid, unexpired authentication token
- `Cluster.IsInDashboardMode` tracks whether the cluster is in dashboard or login mode
- `Cluster.SelectedModule` stores the last selected module for that cluster
- Clusters maintain state even when switching between them
- On deselection, all cluster data (credentials, tokens, module selection) is cleared

**Credentials Flow:**
- `LoginCredentials` model in `MainViewModel` holds the currently active credentials and list of selected clusters
- When a cluster tag is clicked, its credentials are loaded into `MainViewModel.Credentials`
- Login updates both the cluster's stored credentials/token AND the main credentials object
- Failed authentication clears credentials and input fields

### Commands and RelayCommand

All user interactions are handled via the Command pattern:
- `RelayCommand` for parameterless commands
- `RelayCommand<T>` for commands with parameters
- Both classes defined in MainViewModel.cs
- Commands automatically trigger `CanExecute` reevaluation via `CommandManager.RequerySuggested`

### View-ViewModel Mapping

Views are automatically resolved from ViewModels using DataTemplates:
- Defined in `App.xaml` (lines 23-31)
- `PanelViewModel` → `PanelView`
- `LoginViewModel` → `LoginView`
- `DashboardViewModel` → `DashboardView`
- Content is displayed via ContentPresenter bound to ViewModel properties

### Custom Window Chrome

The application uses a custom window with:
- No default Windows chrome (WindowStyle="None", AllowsTransparency="True")
- Custom title bar with drag functionality
- Custom minimize/maximize/close buttons
- Rounded corners and modern styling

## Project Structure

```
Xcelerator/                     # Main WPF Application
├── Models/
│   ├── Cluster.cs              # Cluster data model with auth credentials and token
│   └── LoginCredentials.cs     # Active credentials and selected clusters list
├── ViewModels/
│   ├── BaseViewModel.cs        # INotifyPropertyChanged implementation
│   ├── MainViewModel.cs        # Root ViewModel, navigation container, RelayCommand definitions
│   ├── PanelViewModel.cs       # Cluster selection and dynamic view switching
│   ├── LoginViewModel.cs       # Per-cluster async authentication via IAuthService
│   └── DashboardViewModel.cs   # Module selection and navigation
├── Views/
│   ├── PanelView.xaml          # Cluster selection panel and dynamic content area
│   ├── LoginView.xaml          # Authentication form with error messages
│   ├── DashboardView.xaml      # Module navigation buttons
│   └── MainWindowView.xaml     # Additional view component
├── Converters/
│   ├── CountToVisibilityInvertedConverter.cs
│   ├── InverseBooleanConverter.cs
│   └── StringToVisibilityConverter.cs
├── MainWindow.xaml             # Application shell with custom chrome
├── App.xaml                    # Entry point, Material Design themes, DataTemplates
├── App.xaml.cs                 # DI container configuration, app lifecycle
└── AUTHENTICATION_USAGE.md     # Guide for using authenticated API calls

Xcelerator.NiceClient/          # API Client Library
├── Models/
│   └── AuthToken.cs            # OAuth2 token response model
└── Services/
    └── Auth/
        ├── IAuthService.cs     # Authentication service interface
        └── AuthService.cs      # OAuth2 authentication implementation
```

## Dependencies

### Xcelerator (Main Application)
- **Microsoft.Xaml.Behaviors.Wpf** (1.1.77) - XAML behaviors and interactions
- **MaterialDesignThemes** (4.9.0) - Material Design styling components
- **MaterialDesignColors** (2.1.4) - Material Design color palettes
- **Microsoft.Extensions.Hosting** (10.0.1) - Dependency injection and application hosting
- **Target Framework**: .NET 8.0 Windows
- **Project Reference**: Xcelerator.NiceClient

### Xcelerator.NiceClient (API Client Library)
- **Microsoft.Extensions.Http** (10.0.1) - HttpClient factory and typed clients
- **Target Framework**: .NET 8.0

## Key Implementation Notes

### Adding New Clusters
Modify `PanelViewModel.InitializeClusters()` method (PanelViewModel.cs:86-94)

### Adding New Modules
Module buttons are dynamically generated from the modules defined in DashboardView.xaml. To add new modules, modify the Buttons section in DashboardView.xaml that contains the module definitions (ContactOrchestrator, ContactForge, AgentForge, ConnectGrid, PulseOps).

### Property Change Notifications
All ViewModels inherit from `BaseViewModel` which provides:
- `SetProperty<T>()` helper that only raises PropertyChanged if value actually changed
- Automatic caller member name via `[CallerMemberName]`
- Standard INotifyPropertyChanged implementation

### Command Execution
Commands use the pattern:
```csharp
SomeCommand = new RelayCommand(Execute, CanExecute);
// or with parameter
SomeCommand = new RelayCommand<T>(Execute, CanExecute);
```

### Async/Await Pattern
The application uses async/await for I/O operations:
- Authentication calls use `async Task` methods
- `LoginViewModel.SignIn()` is `async void` (safe for event handlers/commands)
- Error handling with try-catch-finally blocks
- Loading state management during async operations

### Making Authenticated API Calls
After authentication, each cluster has a valid token. To make API calls:
1. Inject `IAuthService` or create HttpClient with token from cluster
2. Check `cluster.HasValidToken` before API calls
3. Use `cluster.AuthToken` and `cluster.ResourceServerBaseUri` for authenticated requests
4. Handle token expiration and re-authentication

See `AUTHENTICATION_USAGE.md` for detailed examples and best practices.
