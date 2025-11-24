using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using TradeDataStudio.Core.Interfaces;

namespace TradeDataStudio.Desktop.ViewModels.Parts;

/// <summary>
/// Manages database connection status display and updates.
/// </summary>
public partial class ConnectionStatusViewModel : ObservableObject
{
    private readonly IConfigurationService _configurationService;
    private readonly IDatabaseService _databaseService;

    [ObservableProperty]
    private string _connectionStatus = "Not connected";

    [ObservableProperty]
    private string _connectedServer = "";

    [ObservableProperty]
    private string _connectedDatabase = "";

    [ObservableProperty]
    private string _connectedUser = "";

    public ConnectionStatusViewModel(
        IConfigurationService configurationService,
        IDatabaseService databaseService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }

    /// <summary>
    /// Updates connection status by testing database connectivity.
    /// </summary>
    public async Task UpdateConnectionStatusAsync()
    {
        try
        {
            var dbConfig = await _configurationService.GetDatabaseConfigurationAsync();
            var isConnected = await _databaseService.TestConnectionAsync();
            
            if (isConnected)
            {
                ConnectionStatus = "Connected";
                ConnectedServer = dbConfig.Server;
                ConnectedDatabase = dbConfig.Database;
                ConnectedUser = dbConfig.UseWindowsAuthentication ? 
                    Environment.UserName : 
                    (!string.IsNullOrEmpty(dbConfig.Username) ? dbConfig.Username : "SQL Server");
            }
            else
            {
                ResetConnectionStatus();
            }
        }
        catch
        {
            ConnectionStatus = "Connection error";
            ConnectedServer = "";
            ConnectedDatabase = "";
            ConnectedUser = "";
        }
    }

    private void ResetConnectionStatus()
    {
        ConnectionStatus = "Not connected";
        ConnectedServer = "";
        ConnectedDatabase = "";
        ConnectedUser = "";
    }
}
