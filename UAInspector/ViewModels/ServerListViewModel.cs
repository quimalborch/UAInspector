using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using UAInspector.Core.Models;
using UAInspector.Core.Services;

namespace UAInspector.ViewModels
{
  /// <summary>
    /// ViewModel for the server list/discovery view
  /// </summary>
    public class ServerListViewModel : ViewModelBase
    {
private readonly StorageService _storageService;
private readonly MainViewModel _mainViewModel;
        private readonly OpcClientService _opcClientService;
        private readonly DiscoveryService _discoveryService;
 private OpcServerInfo _selectedServer;
    private string _manualUrl;
private bool _isDiscovering;
  private string _discoveryStatus;

  public ObservableCollection<OpcServerInfo> Servers { get; }
      public ObservableCollection<OpcServerInfo> DiscoveredServers { get; }

     public OpcServerInfo SelectedServer
   {
    get => _selectedServer;
     set => SetProperty(ref _selectedServer, value);
 }

   public string ManualUrl
     {
  get => _manualUrl;
    set => SetProperty(ref _manualUrl, value);
    }

    public bool IsDiscovering
   {
get => _isDiscovering;
       set => SetProperty(ref _isDiscovering, value);
 }

      public string DiscoveryStatus
 {
       get => _discoveryStatus;
 set => SetProperty(ref _discoveryStatus, value);
  }

    /// <summary>
        /// Get connection state from MainViewModel
   /// </summary>
        public bool IsConnected => _mainViewModel.IsConnected;

        /// <summary>
 /// Get connected server from MainViewModel
        /// </summary>
        public OpcServerInfo ConnectedServer => _mainViewModel.CurrentServer;

  // Commands
public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
public ICommand AddManualCommand { get; }
  public ICommand DiscoverServersCommand { get; }
    public ICommand DeleteServerCommand { get; }
public ICommand RefreshCommand { get; }

  public ServerListViewModel(StorageService storageService, MainViewModel mainViewModel)
    {
  _storageService = storageService;
     _mainViewModel = mainViewModel;
    // Use shared OpcClientService from MainViewModel
  _opcClientService = mainViewModel.OpcClientService;
   _discoveryService = new DiscoveryService();

    Servers = new ObservableCollection<OpcServerInfo>();
    DiscoveredServers = new ObservableCollection<OpcServerInfo>();

        // Subscribe to MainViewModel connection state changes
_mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

   // Initialize commands
     ConnectCommand = new RelayCommand(Connect, () => !IsConnected && SelectedServer != null);
  DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);
  AddManualCommand = new RelayCommand(AddManualServer, () => !IsConnected && !string.IsNullOrWhiteSpace(ManualUrl));
    DiscoverServersCommand = new RelayCommand(DiscoverServers, () => !IsConnected && !IsDiscovering);
  DeleteServerCommand = new RelayCommand<OpcServerInfo>(DeleteServer, (server) => !IsConnected);
       RefreshCommand = new RelayCommand(LoadServers);

 // Load saved servers
    LoadServers();
  }

        private void MainViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
 {
          if (e.PropertyName == nameof(MainViewModel.IsConnected) || 
       e.PropertyName == nameof(MainViewModel.CurrentServer))
     {
    // Notify UI of connection state changes
      OnPropertyChanged(nameof(IsConnected));
    OnPropertyChanged(nameof(ConnectedServer));
  
  // Update command states
   (ConnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
  (DisconnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
     (AddManualCommand as RelayCommand)?.RaiseCanExecuteChanged();
   (DeleteServerCommand as RelayCommand<OpcServerInfo>)?.RaiseCanExecuteChanged();
    (DiscoverServersCommand as RelayCommand)?.RaiseCanExecuteChanged();
   }
 }

  private void LoadServers()
        {
      Servers.Clear();
     var servers = _storageService.LoadServers();
 foreach (var server in servers)
      {
    Servers.Add(server);
   }
 }

  private async void Connect()
        {
if (SelectedServer == null)
     return;

     try
 {
// For now, use anonymous login
   // TODO: Show login dialog to get credentials
       var success = await _opcClientService.ConnectAsync(
               SelectedServer,
    LoginType.Anonymous
     );

        if (success)
     {
    _mainViewModel.OnConnected(SelectedServer);
System.Diagnostics.Debug.WriteLine($"Connected to {SelectedServer.Name}");
                
    System.Windows.MessageBox.Show(
                $"Successfully connected to {SelectedServer.Name}",
    "Connection Success",
         System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }
       else
     {
        System.Diagnostics.Debug.WriteLine($"Failed to connect to {SelectedServer.Name}");
   System.Windows.MessageBox.Show(
  $"Failed to connect to {SelectedServer.Name}",
        "Connection Error",
      System.Windows.MessageBoxButton.OK,
  System.Windows.MessageBoxImage.Error);
       }
  }
    catch (Exception ex)
      {
     System.Diagnostics.Debug.WriteLine($"Connection error: {ex.Message}");
         System.Windows.MessageBox.Show(
   $"Connection error: {ex.Message}",
        "Connection Error",
 System.Windows.MessageBoxButton.OK,
     System.Windows.MessageBoxImage.Error);
}
  }

        private async void Disconnect()
        {
        if (!IsConnected)
        return;

         try
        {
  await _opcClientService.DisconnectAsync();

              var serverName = ConnectedServer?.Name ?? "server";
     
            _mainViewModel.OnDisconnected();
          
   System.Diagnostics.Debug.WriteLine($"Disconnected from {serverName}");

    System.Windows.MessageBox.Show(
      $"Disconnected from {serverName}",
        "Disconnected",
             System.Windows.MessageBoxButton.OK,
       System.Windows.MessageBoxImage.Information);
      }
    catch (Exception ex)
   {
       System.Diagnostics.Debug.WriteLine($"Disconnect error: {ex.Message}");
     System.Windows.MessageBox.Show(
    $"Disconnect error: {ex.Message}",
 "Disconnect Error",
      System.Windows.MessageBoxButton.OK,
   System.Windows.MessageBoxImage.Error);
  }
      }

      private void AddManualServer()
     {
    if (string.IsNullOrWhiteSpace(ManualUrl))
     return;

      // Validate URL format
if (!ManualUrl.Trim().StartsWith("opc.tcp://", StringComparison.OrdinalIgnoreCase))
   {
    System.Windows.MessageBox.Show(
      "Server URL must start with 'opc.tcp://'",
      "Invalid URL",
      System.Windows.MessageBoxButton.OK,
    System.Windows.MessageBoxImage.Warning);
 return;
    }

  var server = new OpcServerInfo
     {
 Id = Guid.NewGuid().ToString(),
   Name = ExtractServerNameFromUrl(ManualUrl.Trim()),
        Url = ManualUrl.Trim(),
   Manufacturer = "Unknown",
 ProductName = "Unknown",
  SecurityMode = "None",
  SecurityPolicy = "None",
   LastConnected = DateTime.MinValue,
      IsFavorite = false,
   LastLoginType = LoginType.Anonymous
     };

   _storageService.AddOrUpdateServer(server);
   LoadServers();
      ManualUrl = string.Empty;
   }

     private string ExtractServerNameFromUrl(string url)
    {
    try
        {
  var uri = new Uri(url);
        return $"{uri.Host}:{uri.Port}";
          }
      catch
   {
   return "Manual Server";
      }
        }

        private async void DiscoverServers()
  {
  IsDiscovering = true;
       DiscoveryStatus = "Scanning network for OPC UA servers...";
DiscoveredServers.Clear();

     try
{
        // Discover on common ports
    DiscoveryStatus = "Checking common OPC UA ports...";
      var servers = await _discoveryService.DiscoverServersOnNetworkAsync();
  
      if (servers.Count == 0)
 {
      // Try LDS discovery
        DiscoveryStatus = "Checking Local Discovery Server...";
        servers = await _discoveryService.DiscoverServersWithLDSAsync();
 }

         if (servers.Count > 0)
   {
    DiscoveryStatus = $"Found {servers.Count} server(s)";
     foreach (var server in servers)
  {
   DiscoveredServers.Add(server);
          }
      }
   else
     {
   DiscoveryStatus = "No servers found";
      System.Windows.MessageBox.Show(
     "No OPC UA servers found on the network.\n\n" +
   "Make sure:\n" +
     "- An OPC UA server is running\n" +
      "- The server is accessible on the network\n" +
    "- Firewall allows OPC UA communication",
    "Discovery Result",
 System.Windows.MessageBoxButton.OK,
     System.Windows.MessageBoxImage.Information);
    }
       }
    catch (Exception ex)
         {
  DiscoveryStatus = "Discovery failed";
System.Diagnostics.Debug.WriteLine($"Discovery error: {ex.Message}");
   System.Windows.MessageBox.Show(
  $"Discovery error: {ex.Message}",
   "Discovery Error",
    System.Windows.MessageBoxButton.OK,
  System.Windows.MessageBoxImage.Error);
  }
   finally
  {
  IsDiscovering = false;
     }
  }

   private void DeleteServer(OpcServerInfo server)
{
  if (server == null)
     return;

        var result = System.Windows.MessageBox.Show(
    $"Are you sure you want to delete '{server.Name}'?",
   "Confirm Delete",
System.Windows.MessageBoxButton.YesNo,
     System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
        {
       var servers = _storageService.LoadServers();
     servers.RemoveAll(s => s.Id == server.Id);
  _storageService.SaveServers(servers);
   LoadServers();
    }
  }
    }
}
