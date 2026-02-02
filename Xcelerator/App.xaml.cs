using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using Velopack;
using Xcelerator.NiceClient.Services.Auth;
using Xcelerator.Services;
using Xcelerator.ViewModels;

namespace Xcelerator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }

        public App()
        {
            var builder = Host.CreateApplicationBuilder();

            // Clear default logging providers and reconfigure without EventLog
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();

            // --- 1. Register the NICE Services ---
            builder.Services.AddHttpClient<IAuthService, AuthService>();

            // --- 2. Register Application Services ---
            builder.Services.AddSingleton<LogFileManager>();

            // --- 3. Register ViewModels ---
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<MainWindow>();

            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<PanelViewModel>();

            AppHost = builder.Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // Velopack must be initialized first, before anything else
            VelopackApp.Build().Run();

            await AppHost!.StartAsync();

            var startupForm = AppHost.Services.GetRequiredService<MainWindow>();

            // Optional: Automatically set DataContext if you haven't done it in XAML
            startupForm.DataContext = AppHost.Services.GetRequiredService<MainViewModel>();

            startupForm.Show();
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // Clean up all downloaded log files before exiting
            try
            {
                var logFileManager = AppHost?.Services.GetService<LogFileManager>();
                if (logFileManager != null)
                {
                    var stats = logFileManager.CleanupAllLogFiles();
                    System.Diagnostics.Debug.WriteLine($"Application exit cleanup: {stats}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during cleanup on exit: {ex.Message}");
            }

            // Gracefully stop the host (cleans up connections/services)
            await AppHost!.StopAsync();
            base.OnExit(e);
        }
    }
}

