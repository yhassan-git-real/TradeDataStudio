using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Services;
using TradeDataStudio.Desktop.ViewModels;
using TradeDataStudio.Desktop.Views;

namespace TradeDataStudio.Desktop;

public partial class App : Application
{
    private IHost? _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Configure services and dependency injection
        _host = CreateHost();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Get the main window with injected dependencies
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
            
            // Handle application shutdown
            desktop.ShutdownRequested += (sender, e) =>
            {
                _host?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }


    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register core services
                services.AddSingleton<ILoggingService, LoggingService>();
                services.AddSingleton<IConfigurationService, TradeDataStudio.Core.Services.ConfigurationService>();
                services.AddTransient<IDatabaseService, OptimizedDatabaseService>();
                services.AddTransient<IExportService, ExportService>();
                services.AddTransient<IStoredProcedureValidator, StoredProcedureValidator>();

                // Register ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Register Views
                services.AddTransient<MainWindow>();
                services.AddTransient<SettingsWindow>();
            })
            .Build();
    }
}