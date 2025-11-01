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
        private readonly UpdateService _updateService;
        private ViewModelBase _currentViewModel;
        private string _statusMessage;
        private bool _isConnected;
        private OpcServerInfo _currentServer;
        private bool _isUpdateAvailable;

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

        /// <summary>
        /// Indicates if an update is available
        /// </summary>
        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set => SetProperty(ref _isUpdateAvailable, value);
        }

        // Commands
        public ICommand ShowServerListCommand { get; }
        public ICommand ShowExplorerCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ApplyUpdateCommand { get; }
        public ICommand ExitCommand { get; }

        public MainViewModel()
        {
            _storageService = new StorageService();
            _opcClientService = new OpcClientService();
            _updateService = new UpdateService(this);
            RecentServers = new ObservableCollection<OpcServerInfo>();
            Settings = _storageService.LoadSettings();

            // Initialize commands
            ShowServerListCommand = new RelayCommand(ShowServerList);
            ShowExplorerCommand = new RelayCommand(ShowExplorer, () => IsConnected);
            ShowSettingsCommand = new RelayCommand(ShowSettings);
            ApplyUpdateCommand = new RelayCommand(async () => await ApplyUpdate(), () => IsUpdateAvailable);
            ExitCommand = new RelayCommand(Exit);

            // Initialize OPC client
            InitializeOpcClient();

            // Load data
            LoadRecentServers();

            // Show server list by default
            ShowServerList();
            StatusMessage = "Ready - Select or add a server to connect";

            // Check for updates on startup
            _ = CheckForUpdatesAsync();
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

        /// <summary>
        /// Check for updates on startup
        /// </summary>
        private async System.Threading.Tasks.Task CheckForUpdatesAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Checking for updates...");
                IsUpdateAvailable = await _updateService.CheckForUpdatesAsync();

                if (IsUpdateAvailable)
                {
                    var version = _updateService.GetPendingUpdateVersion();
                    System.Diagnostics.Debug.WriteLine($"Update available: {version}");
                    StatusMessage = $"Update available: v{version}";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No updates available");
                }

                  // Refresh command state
                  (ApplyUpdateCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking for updates: {ex.Message}");
            }
        }

        /// <summary>
        /// Download and apply the pending update
        /// </summary>
        private async System.Threading.Tasks.Task ApplyUpdate()
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
             "A new update is available. The application will restart to apply the update.\n\nDo you want to continue?",
              "Update Available",
                    System.Windows.MessageBoxButton.YesNo,
             System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    StatusMessage = "Downloading update...";

                    // Disconnect if connected
                    if (IsConnected && _opcClientService != null)
                    {
                        await _opcClientService.DisconnectAsync();
                    }

                    // Download and apply updates
                    await _updateService.DownloadAndApplyUpdatesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying update: {ex.Message}");
                System.Windows.MessageBox.Show(
                 $"Failed to apply update: {ex.Message}",
              "Update Error",
        System.Windows.MessageBoxButton.OK,
             System.Windows.MessageBoxImage.Error);
            }
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