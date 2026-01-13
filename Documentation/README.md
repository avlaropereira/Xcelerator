# Xcelerator WPF Application

A modern Windows-inspired WPF application built with C# and .NET 8, featuring a multi-step workflow for cluster selection, authentication, and dashboard navigation.

## Features

### 🎨 Modern UI Design
- **Windows-inspired styling** with rounded corners, subtle gradients, and soft shadows
- **Custom window chrome** with minimize and close buttons
- **Responsive layout** that adapts to window resizing
- **Light theme** with modern color palette

### 🔄 Multi-Step Workflow
1. **Cluster Selection Page**
   - Multi-select dropdown for clusters (sc1, sc10, etc.)
   - Selected clusters displayed as pill-shaped tags
   - Continue button enabled only when clusters are selected

2. **Login Page**
   - Access key and Secret key input fields
   - Password masking for secret key
   - Sign in button with validation
   - Selected clusters displayed in left panel

3. **Dashboard Page**
   - Navigation buttons for different modules:
     - ContactOrchestrator
     - ContactForge
     - AgentForge
     - ConnectGrid
     - PulseOps
   - Go back functionality
   - Content placeholder area

### 🏗️ Architecture
- **MVVM Pattern** with proper separation of concerns
- **Data binding** with INotifyPropertyChanged
- **Command pattern** for user interactions
- **Navigation system** with smooth transitions

## Project Structure

```
Xcelerator/
├── Models/
│   ├── Cluster.cs              # Cluster data model
│   └── LoginCredentials.cs     # Login credentials model
├── ViewModels/
│   ├── BaseViewModel.cs        # Base ViewModel with INotifyPropertyChanged
│   ├── MainViewModel.cs        # Main navigation ViewModel
│   ├── ClusterSelectionViewModel.cs
│   ├── LoginViewModel.cs
│   └── DashboardViewModel.cs
├── Views/
│   ├── ClusterSelectionView.xaml
│   ├── LoginView.xaml
│   └── DashboardView.xaml
├── MainWindow.xaml             # Main application window
└── App.xaml                    # Application entry point
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension

### Building and Running

1. **Clone or download the project**
2. **Navigate to the project directory**
   ```bash
   cd Xcelerator
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

### Usage Instructions

1. **Cluster Selection**
   - Double-click on clusters from the list to select them
   - Selected clusters appear as blue tags below
   - Click the "✕" button on tags to remove them
   - Click "Continue" when ready

2. **Login**
   - Enter your Access Key (username)
   - Enter your Secret Key (password)
   - Click "Sign in" to proceed
   - For demo purposes, any non-empty credentials are accepted

3. **Dashboard**
   - Click on any module button to select it
   - Use "Go Back" to return to previous screens
   - The selected module is displayed at the bottom

## Technical Details

### Dependencies
- **Microsoft.Xaml.Behaviors.Wpf** - For advanced XAML behaviors
- **.NET 8.0** - Target framework

### Key Features
- **Custom Window Styling** - Rounded corners, custom title bar
- **Responsive Design** - Grid-based layout with proportional sizing
- **Data Validation** - Input validation and button state management
- **Smooth Navigation** - Seamless transitions between pages

### Design Patterns
- **MVVM (Model-View-ViewModel)** - Clean separation of UI and business logic
- **Command Pattern** - Decoupled user interactions
- **Observer Pattern** - Property change notifications
- **Factory Pattern** - ViewModel creation

## Customization

### Adding New Clusters
Edit the `InitializeClusters()` method in `ClusterSelectionViewModel.cs`:

```csharp
private void InitializeClusters()
{
    AvailableClusters.Clear();
    // Add your clusters here
    for (int i = 1; i <= 20; i++)
    {
        AvailableClusters.Add(new Cluster($"sc{i}", $"SC{i}"));
    }
}
```

### Adding New Modules
Edit the `AvailableModules` array in `DashboardViewModel.cs`:

```csharp
public string[] AvailableModules { get; } = new[]
{
    "ContactOrchestrator",
    "ContactForge", 
    "AgentForge",
    "ConnectGrid",
    "PulseOps",
    "YourNewModule"  // Add here
};
```

### Styling
All styles are defined in the XAML files. Key style resources:
- `ModernButtonStyle` - Primary action buttons
- `ModernTextBoxStyle` - Input fields
- `NavigationButtonStyle` - Dashboard navigation
- `ClusterTagStyle` - Selected cluster tags

## Future Enhancements

- [ ] Dark mode support
- [ ] Real authentication integration
- [ ] Module-specific content pages
- [ ] Settings and configuration
- [ ] Data persistence
- [ ] Animation transitions
- [ ] Accessibility improvements

## License

This project is created for demonstration purposes.

## Support

For issues or questions, please refer to the code comments or create an issue in the repository.
