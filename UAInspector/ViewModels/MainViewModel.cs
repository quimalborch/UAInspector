using System;
using System.Collections.ObjectModel;
using System.Linq;
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
   private ViewModelBase _currentViewModel;
   private string _statusMessage;
   private bool _isConnected;
   private OpcServerInfo _currentServer;

  public ObservableCollection<OpcServerInfo> RecentServers { get; }
    public AppSettings Settings { get; private set; }

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
   set => SetProperty(ref _isConnected, value);
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
      RecentServers = new ObservableCollection<OpcServerInfo>();
      Settings = _storageService.LoadSettings();

   // Initialize commands
  ShowServerListCommand = new RelayCommand(ShowServerList);
    ShowExplorerCommand = new RelayCommand(ShowExplorer, () => IsConnected);
  ShowSettingsCommand = new RelayCommand(ShowSettings);
     ExitCommand = new RelayCommand(Exit);

      // Load data
 LoadRecentServers();

    // Show server list by default
       ShowServerList();
StatusMessage = "Ready - Select or add a server to connect";
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

   // TODO: Implement ExplorerViewModel
 // CurrentViewModel = new ExplorerViewModel(_storageService, this);
        StatusMessage = $"Connected to {CurrentServer?.Name ?? "server"} - Browsing address space";
        
 System.Windows.MessageBox.Show(
       "Explorer view is not yet implemented.\n\n" +
     "This will show the OPC UA node tree browser.",
"Coming Soon",
    System.Windows.MessageBoxButton.OK,
     System.Windows.MessageBoxImage.Information);
  }

  private void ShowSettings()
        {
    // TODO: Implement SettingsViewModel
     // CurrentViewModel = new SettingsViewModel(_storageService, Settings);
  StatusMessage = "Settings";
        
    System.Windows.MessageBox.Show(
      "Settings view is not yet implemented.\n\n" +
  "This will allow you to configure:\n" +
  "- Connection timeouts\n" +
   "- Subscription settings\n" +
 "- Certificate management\n" +
   "- UI preferences",
 "Coming Soon",
System.Windows.MessageBoxButton.OK,
      System.Windows.MessageBoxImage.Information);
   }

  private void Exit()
  {
System.Windows.Application.Current.Shutdown();
  }

    public void OnConnected(OpcServerInfo serverInfo)
  {
       IsConnected = true;
            CurrentServer = serverInfo;
    StatusMessage = $"Connected to {serverInfo.Name} ({serverInfo.Url})";
      _storageService.AddOrUpdateServer(serverInfo);
LoadRecentServers();
        
        // Notify commands to re-evaluate CanExecute
        (ShowExplorerCommand as RelayCommand)?.RaiseCanExecuteChanged();
 }

public void OnDisconnected()
  {
 IsConnected = false;
 CurrentServer = null;
StatusMessage = "Disconnected - Select a server to reconnect";
     
        // Notify commands to re-evaluate CanExecute
   (ShowExplorerCommand as RelayCommand)?.RaiseCanExecuteChanged();
        
 // Go back to server list
     ShowServerList();
  }
    }
}
