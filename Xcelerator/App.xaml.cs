using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using Xcelerator.NiceClient.Services.Auth;
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

            // --- 1. Register the NICE Services ---
            builder.Services.AddHttpClient<IAuthService, AuthService>();

            // --- 2. Register ViewModels ---
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<MainWindow>();

            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<PanelViewModel>();

            AppHost = builder.Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await AppHost!.StartAsync();

            var startupForm = AppHost.Services.GetRequiredService<MainWindow>();

            // Optional: Automatically set DataContext if you haven't done it in XAML
            startupForm.DataContext = AppHost.Services.GetRequiredService<MainViewModel>();

            startupForm.Show();
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            // Gracefully stop the host (cleans up connections/services)
            await AppHost!.StopAsync();
            base.OnExit(e);
        }
    }
}
