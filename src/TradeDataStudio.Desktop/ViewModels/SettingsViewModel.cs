using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TradeDataStudio.Core.Interfaces;
using TradeDataStudio.Core.Models;

namespace TradeDataStudio.Desktop.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private string _server = "";

    [ObservableProperty]
    private string _database = "";

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private bool _useWindowsAuthentication = true;

    [ObservableProperty]
    private int _connectionTimeout = 30;

    [ObservableProperty]
    private bool _trustServerCertificate = true;

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private bool _isTestingConnection = false;

    [ObservableProperty]
    private bool _isAuthenticationExpanded = true;

    public string AuthenticationChevron => IsAuthenticationExpanded ? "▼" : "▶";

    public ICommand TestConnectionCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ToggleAuthenticationCommand { get; }

    public event EventHandler? CloseRequested;

    public SettingsViewModel(IConfigurationService configurationService, IDatabaseService databaseService)
    {
        _configurationService = configurationService;
        _databaseService = databaseService;

        TestConnectionCommand = new AsyncRelayCommand(TestConnectionAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        CancelCommand = new RelayCommand(Cancel);
        ToggleAuthenticationCommand = new RelayCommand(ToggleAuthentication);

        _ = LoadCurrentSettingsAsync();
    }

    private async Task LoadCurrentSettingsAsync()
    {
        try
        {
            var dbConfig = await _configurationService.GetDatabaseConfigurationAsync();
            Server = dbConfig.Server;
            Database = dbConfig.Database;
            Username = dbConfig.Username;
            Password = dbConfig.Password;
            UseWindowsAuthentication = dbConfig.UseWindowsAuthentication;
            ConnectionTimeout = dbConfig.ConnectionTimeout;
            TrustServerCertificate = dbConfig.TrustServerCertificate;
            StatusMessage = "Settings loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load settings: {ex.Message}";
        }
    }

    private async Task TestConnectionAsync()
    {
        try
        {
            IsTestingConnection = true;
            StatusMessage = "Testing connection...";

            // Temporarily save config for testing
            var testConfig = new DatabaseConfiguration
            {
                Server = Server,
                Database = Database,
                Username = Username,
                Password = Password,
                UseWindowsAuthentication = UseWindowsAuthentication,
                ConnectionTimeout = ConnectionTimeout,
                TrustServerCertificate = TrustServerCertificate
            };

            await _configurationService.SaveDatabaseConfigurationAsync(testConfig);
            var result = await _databaseService.TestConnectionDetailedAsync();

            if (result.Success)
            {
                StatusMessage = $"✅ {result.Message} (in {result.TestDuration.TotalMilliseconds:F0}ms)";
            }
            else
            {
                StatusMessage = $"❌ {result.Message}";
                if (!string.IsNullOrEmpty(result.ErrorDetails) && result.ErrorDetails != result.Message)
                {
                    StatusMessage += $" - {result.ErrorDetails}";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Connection test failed: {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            var config = new DatabaseConfiguration
            {
                Server = Server,
                Database = Database,
                Username = Username,
                Password = Password,
                UseWindowsAuthentication = UseWindowsAuthentication,
                ConnectionTimeout = ConnectionTimeout,
                TrustServerCertificate = TrustServerCertificate
            };

            await _configurationService.SaveDatabaseConfigurationAsync(config);
            StatusMessage = "✅ Settings saved successfully!";

            // Close the window after a brief delay
            await Task.Delay(1000);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Failed to save settings: {ex.Message}";
        }
    }

    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ToggleAuthentication()
    {
        IsAuthenticationExpanded = !IsAuthenticationExpanded;
        OnPropertyChanged(nameof(AuthenticationChevron));
    }
}