using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using UAInspector.Core.Models;
using UAInspector.Core.Services;

namespace UAInspector.ViewModels
{
  /// <summary>
    /// ViewModel for the main application window
 /// </summary>
  public class MainViewModel : ViewModelBase
 {
    private readonly StorageService _storageService;
 private readonly OpcClientService _opcClientService;
    private ViewModelBase _currentViewModel;
    private string _statusMessage;
    private bool _isConnected;
    private OpcServerInfo _currentServer;

  public ObservableCollection<OpcServerInfo> RecentServers { get; }
    public AppSettings Settings { get; set; }

    /// <summary>
    /// Application version from assembly
    /// </summary>
    public string AppVersion
    {
        get
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                return $"v{version.Major}.{version.Minor}.{version.Build}";
            }
            catch
            {
                return "ERROR VERSION";
            }
        }
    }

        /// <summary>
 /// Shared OPC Client Service instance
    /// </summary>
 public OpcClientService OpcClientService => _opcClientService;

  public ViewModelBase CurrentViewModel
  {
  get => _currentViewModel;
    set => SetProperty(ref _currentViewModel, value);
  }

  public string StatusMessage
{
   get => _statusMessage;
 set => SetProperty(ref _statusMessage, value);
   }

     public bool IsConnected
 {
  get => _isConnected;
   set
   {
      if (SetProperty(ref _isConnected, value))
 {
  // Notify Explorer command to update
        (ShowExplorerCommand as RelayCommand)?.RaiseCanExecuteChanged();
}
}
 }

   public OpcServerInfo CurrentServer
    {
   get => _currentServer;
    set => SetProperty(ref _currentServer, value);
 }

      // Commands
 public ICommand ShowServerListCommand { get; }
  public ICommand ShowExplorerCommand { get; }
 public ICommand ShowSettingsCommand { get; }
     public ICommand ExitCommand { get; }

   public MainViewModel()
  {
       _storageService = new StorageService();
   _opcClientService = new OpcClientService();
RecentServers = new ObservableCollection<OpcServerInfo>();
      Settings = _storageService.LoadSettings();

   // Initialize commands
  ShowServerListCommand = new RelayCommand(ShowServerList);
    ShowExplorerCommand = new RelayCommand(ShowExplorer, () => IsConnected);
  ShowSettingsCommand = new RelayCommand(ShowSettings);
     ExitCommand = new RelayCommand(Exit);

        // Initialize OPC client
  InitializeOpcClient();

      // Load data
 LoadRecentServers();

    // Show server list by default
    ShowServerList();
StatusMessage = "Ready - Select or add a server to connect";
    }

        private async void InitializeOpcClient()
        {
            try
       {
        await _opcClientService.InitializeAsync();
         System.Diagnostics.Debug.WriteLine("OPC UA client initialized in MainViewModel");
  }
            catch (Exception ex)
  {
    System.Diagnostics.Debug.WriteLine($"Failed to initialize OPC UA client: {ex.Message}");
    }
    }

  private void LoadRecentServers()
   {
    RecentServers.Clear();
      var servers = _storageService.LoadServers();
 foreach (var server in servers.Take(10))
      {
   RecentServers.Add(server);
      }
  }

 private void ShowServerList()
    {
        // Reuse existing ServerListViewModel if possible, or create new one with shared service
     CurrentViewModel = new ServerListViewModel(_storageService, this);
        StatusMessage = IsConnected 
   ? $"Connected to {CurrentServer?.Name ?? "server"} - Viewing server list"
     : "Select or add a server to connect";
 }

 private void ShowExplorer()
   {
  if (!IsConnected)
      {
      StatusMessage = "Not connected to any server";
   return;
    }

   // Create ExplorerViewModel with shared OPC client service
 CurrentViewModel = new ExplorerViewModel(_storageService, this);
        StatusMessage = $"Connected to {CurrentServer?.Name ?? "server"} - Browsing address space";
     
 System.Diagnostics.Debug.WriteLine("Explorer view loaded");
  }

  private void ShowSettings()
   {
  // Create SettingsViewModel with shared storage service
     CurrentViewModel = new SettingsViewModel(_storageService, this);
  StatusMessage = "Settings";
     
    System.Diagnostics.Debug.WriteLine("Settings view loaded");
   }

  private void Exit()
  {
 // Disconnect before closing
    if (IsConnected && _opcClientService != null)
 {
 try
   {
_opcClientService.DisconnectAsync().Wait();
 }
  catch (Exception ex)
   {
   System.Diagnostics.Debug.WriteLine($"Error disconnecting on exit: {ex.Message}");
       }
  }
      
System.Windows.Application.Current.Shutdown();
  }

    public void OnConnected(OpcServerInfo serverInfo)
  {
  IsConnected = true;
    CurrentServer = serverInfo;
    StatusMessage = $"Connected to {serverInfo.Name} ({serverInfo.Url})";
  _storageService.AddOrUpdateServer(serverInfo);
LoadRecentServers();
   
   System.Diagnostics.Debug.WriteLine($"MainViewModel: Connection state updated - IsConnected={IsConnected}");
 }

public void OnDisconnected()
  {
 IsConnected = false;
 CurrentServer = null;
StatusMessage = "Disconnected - Select a server to reconnect";
  
      System.Diagnostics.Debug.WriteLine($"MainViewModel: Connection state updated - IsConnected={IsConnected}");
  }
    }
}
