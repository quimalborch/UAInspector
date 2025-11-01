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

  // Commands
        public ICommand ConnectCommand { get; }
public ICommand AddManualCommand { get; }
  public ICommand DiscoverServersCommand { get; }
    public ICommand DeleteServerCommand { get; }
public ICommand RefreshCommand { get; }

  public ServerListViewModel(StorageService storageService, MainViewModel mainViewModel)
      {
  _storageService = storageService;
     _mainViewModel = mainViewModel;
     _opcClientService = new OpcClientService();
     _discoveryService = new DiscoveryService();

      Servers = new ObservableCollection<OpcServerInfo>();
            DiscoveredServers = new ObservableCollection<OpcServerInfo>();

   // Initialize commands
     ConnectCommand = new RelayCommand(Connect, () => SelectedServer != null);
  AddManualCommand = new RelayCommand(AddManualServer, () => !string.IsNullOrWhiteSpace(ManualUrl));
    DiscoverServersCommand = new RelayCommand(DiscoverServers, () => !IsDiscovering);
     DeleteServerCommand = new RelayCommand<OpcServerInfo>(DeleteServer);
         RefreshCommand = new RelayCommand(LoadServers);

     // Initialize OPC client
            InitializeOpcClient();

   // Load saved servers
    LoadServers();
  }

 private async void InitializeOpcClient()
        {
         try
       {
    await _opcClientService.InitializeAsync();
     System.Diagnostics.Debug.WriteLine("OPC UA client initialized successfully");
  }
            catch (Exception ex)
            {
             System.Diagnostics.Debug.WriteLine($"Failed to initialize OPC UA client: {ex.Message}");
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
