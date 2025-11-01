using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Opc.Ua;
using Opc.Ua.Client;
using UAInspector.Core.Models;
using UAInspector.Core.Services;

namespace UAInspector.ViewModels
{
 /// <summary>
    /// ViewModel for OPC UA address space explorer
    /// </summary>
    public class ExplorerViewModel : ViewModelBase
    {
        private readonly StorageService _storageService;
        private readonly MainViewModel _mainViewModel;
   private readonly OpcClientService _opcClientService;
        
        private OpcNodeInfo _selectedNode;
        private bool _isLoading;
   private string _writeValue;
        private bool _isMonitoring;
        private string _searchText;

        public ObservableCollection<OpcNodeInfo> RootNodes { get; }
        public ObservableCollection<OpcNodeInfo> MonitoredNodes { get; }

        public OpcNodeInfo SelectedNode
        {
         get => _selectedNode;
       set
{
                if (SetProperty(ref _selectedNode, value))
         {
            OnPropertyChanged(nameof(IsNodeSelected));
  OnPropertyChanged(nameof(CanWrite));
         OnPropertyChanged(nameof(CanMonitor));
                (ReadValueCommand as RelayCommand)?.RaiseCanExecuteChanged();
               (WriteValueCommand as RelayCommand)?.RaiseCanExecuteChanged();
      (MonitorNodeCommand as RelayCommand)?.RaiseCanExecuteChanged();
    (UnmonitorNodeCommand as RelayCommand)?.RaiseCanExecuteChanged();
       (CopyNodeIdCommand as RelayCommand)?.RaiseCanExecuteChanged();

        // Auto-read value when node is selected
       if (value?.NodeClass == OpcNodeClass.Variable)
            {
  ReadValue();
 }
                }
            }
        }

        public bool IsLoading
      {
      get => _isLoading;
       set => SetProperty(ref _isLoading, value);
        }

        public string WriteValue
        {
         get => _writeValue;
            set => SetProperty(ref _writeValue, value);
        }

     public bool IsMonitoring
   {
    get => _isMonitoring;
set => SetProperty(ref _isMonitoring, value);
        }

    public string SearchText
        {
            get => _searchText;
      set => SetProperty(ref _searchText, value);
}

        public bool IsNodeSelected => SelectedNode != null;
        public bool CanWrite => SelectedNode?.IsWritable == true;
        public bool CanMonitor => SelectedNode?.NodeClass == OpcNodeClass.Variable;

        public string ServerName => _mainViewModel.CurrentServer?.Name ?? "Unknown";
  public string ServerUrl => _mainViewModel.CurrentServer?.Url ?? "";

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand ReadValueCommand { get; }
        public ICommand WriteValueCommand { get; }
 public ICommand MonitorNodeCommand { get; }
  public ICommand UnmonitorNodeCommand { get; }
public ICommand CopyNodeIdCommand { get; }
   public ICommand SearchCommand { get; }
      public ICommand ClearMonitoredCommand { get; }

     public ExplorerViewModel(StorageService storageService, MainViewModel mainViewModel)
        {
       _storageService = storageService;
         _mainViewModel = mainViewModel;
      _opcClientService = mainViewModel.OpcClientService;

     RootNodes = new ObservableCollection<OpcNodeInfo>();
        MonitoredNodes = new ObservableCollection<OpcNodeInfo>();

      // Initialize commands
            RefreshCommand = new RelayCommand(async () => await LoadRootNodesAsync());
            ReadValueCommand = new RelayCommand(ReadValue, () => SelectedNode?.NodeClass == OpcNodeClass.Variable);
 WriteValueCommand = new RelayCommand(WriteValueToNode, () => CanWrite && !string.IsNullOrWhiteSpace(WriteValue));
    MonitorNodeCommand = new RelayCommand(MonitorNode, () => CanMonitor && !IsNodeMonitored(SelectedNode));
            UnmonitorNodeCommand = new RelayCommand(UnmonitorNode, () => IsNodeMonitored(SelectedNode));
 CopyNodeIdCommand = new RelayCommand(CopyNodeId, () => IsNodeSelected);
            SearchCommand = new RelayCommand(Search);
          ClearMonitoredCommand = new RelayCommand(ClearMonitored);

            // Load root nodes
            _ = LoadRootNodesAsync();
     }

        private async Task LoadRootNodesAsync()
      {
     IsLoading = true;
try
  {
            RootNodes.Clear();

       // Browse from root (Objects folder)
            var nodes = await _opcClientService.BrowseAsync(null);

   foreach (var node in nodes)
         {
             RootNodes.Add(node);
            }

     System.Diagnostics.Debug.WriteLine($"Loaded {nodes.Count} root nodes");
     }
     catch (Exception ex)
 {
         System.Diagnostics.Debug.WriteLine($"Error loading root nodes: {ex.Message}");
         System.Windows.MessageBox.Show(
$"Failed to load nodes: {ex.Message}",
         "Error",
          System.Windows.MessageBoxButton.OK,
        System.Windows.MessageBoxImage.Error);
      }
            finally
   {
       IsLoading = false;
       }
        }

        public async Task LoadChildNodesAsync(OpcNodeInfo parentNode)
     {
 if (parentNode == null || parentNode.IsLoaded)
     return;

      try
            {
      parentNode.Children.Clear();

       var nodes = await _opcClientService.BrowseAsync(parentNode.NodeId);

 foreach (var node in nodes)
      {
         parentNode.Children.Add(node);
      }

 parentNode.IsLoaded = true;
           System.Diagnostics.Debug.WriteLine($"Loaded {nodes.Count} children for {parentNode.DisplayName}");
      }
            catch (Exception ex)
 {
         System.Diagnostics.Debug.WriteLine($"Error loading child nodes: {ex.Message}");
     }
        }

        private async void ReadValue()
   {
      if (SelectedNode?.NodeClass != OpcNodeClass.Variable)
     return;

      try
         {
       var dataValue = await _opcClientService.ReadValueAsync(SelectedNode.NodeId);
                if (dataValue != null)
     {
       SelectedNode.Value = dataValue.Value;
    SelectedNode.Timestamp = dataValue.SourceTimestamp;
        SelectedNode.Quality = dataValue.StatusCode.ToString();

// Update UI
      OnPropertyChanged(nameof(SelectedNode));

      System.Diagnostics.Debug.WriteLine($"Read value: {SelectedNode.Value} from {SelectedNode.DisplayName}");
   }
        }
            catch (Exception ex)
   {
    System.Diagnostics.Debug.WriteLine($"Error reading value: {ex.Message}");
             System.Windows.MessageBox.Show(
                    $"Failed to read value: {ex.Message}",
     "Read Error",
       System.Windows.MessageBoxButton.OK,
     System.Windows.MessageBoxImage.Error);
            }
        }

        private async void WriteValueToNode()
      {
         if (!CanWrite || string.IsNullOrWhiteSpace(WriteValue))
  return;

      try
 {
             // Try to parse the value based on data type
  object valueToWrite = ParseValue(WriteValue, SelectedNode.DataType);

       var success = await _opcClientService.WriteValueAsync(SelectedNode.NodeId, valueToWrite);
         
         if (success)
      {
     System.Windows.MessageBox.Show(
          $"Successfully wrote value '{WriteValue}' to {SelectedNode.DisplayName}",
 "Write Success",
      System.Windows.MessageBoxButton.OK,
              System.Windows.MessageBoxImage.Information);

        // Re-read to confirm
        ReadValue();
   WriteValue = string.Empty;
         }
        else
         {
    System.Windows.MessageBox.Show(
     $"Failed to write value to {SelectedNode.DisplayName}",
                 "Write Failed",
        System.Windows.MessageBoxButton.OK,
   System.Windows.MessageBoxImage.Warning);
              }
          }
            catch (Exception ex)
            {
       System.Diagnostics.Debug.WriteLine($"Error writing value: {ex.Message}");
        System.Windows.MessageBox.Show(
    $"Failed to write value: {ex.Message}",
      "Write Error",
           System.Windows.MessageBoxButton.OK,
    System.Windows.MessageBoxImage.Error);
    }
        }

  private object ParseValue(string value, string dataType)
        {
    // Simple parsing - can be enhanced based on OPC UA data types
            if (string.IsNullOrWhiteSpace(dataType))
   return value;

        try
        {
    if (dataType.Contains("Boolean", StringComparison.OrdinalIgnoreCase))
    return bool.Parse(value);
            else if (dataType.Contains("Int32", StringComparison.OrdinalIgnoreCase))
 return int.Parse(value);
    else if (dataType.Contains("Int16", StringComparison.OrdinalIgnoreCase))
            return short.Parse(value);
     else if (dataType.Contains("Int64", StringComparison.OrdinalIgnoreCase))
          return long.Parse(value);
             else if (dataType.Contains("Float", StringComparison.OrdinalIgnoreCase))
 return float.Parse(value);
     else if (dataType.Contains("Double", StringComparison.OrdinalIgnoreCase))
    return double.Parse(value);
  else if (dataType.Contains("UInt32", StringComparison.OrdinalIgnoreCase))
        return uint.Parse(value);
         else if (dataType.Contains("UInt16", StringComparison.OrdinalIgnoreCase))
             return ushort.Parse(value);
 else if (dataType.Contains("Byte", StringComparison.OrdinalIgnoreCase))
    return byte.Parse(value);
 else
        return value; // Default to string
}
   catch
         {
     return value; // Fallback to string
            }
        }

        private async void MonitorNode()
        {
            if (!CanMonitor || IsNodeMonitored(SelectedNode))
       return;

   try
            {
    // Ensure subscription exists
    if (!IsMonitoring)
                {
    await _opcClientService.CreateSubscriptionAsync(1000);
IsMonitoring = true;
      }

                // Add monitored item
         _opcClientService.AddMonitoredItem(SelectedNode.NodeId, (item, e) =>
                {
  // Update value on notification
     if (e.NotificationValue is MonitoredItemNotification notification)
               {
          System.Windows.Application.Current.Dispatcher.Invoke(() =>
 {
      var monitoredNode = MonitoredNodes.FirstOrDefault(n => n.NodeId == SelectedNode.NodeId);
            if (monitoredNode != null)
        {
               monitoredNode.Value = notification.Value.Value;
monitoredNode.Timestamp = notification.Value.SourceTimestamp;
        monitoredNode.Quality = notification.Value.StatusCode.ToString();
         }

          // Also update selected node if it's the same
       if (SelectedNode?.NodeId == item.StartNodeId.ToString())
   {
       SelectedNode.Value = notification.Value.Value;
            SelectedNode.Timestamp = notification.Value.SourceTimestamp;
              SelectedNode.Quality = notification.Value.StatusCode.ToString();
            OnPropertyChanged(nameof(SelectedNode));
     }
             });
         }
   });

             // Add to monitored list
    var nodeToMonitor = new OpcNodeInfo
          {
         NodeId = SelectedNode.NodeId,
              DisplayName = SelectedNode.DisplayName,
     BrowseName = SelectedNode.BrowseName,
    NodeClass = SelectedNode.NodeClass,
         DataType = SelectedNode.DataType,
        Value = SelectedNode.Value,
          Timestamp = SelectedNode.Timestamp,
     Quality = SelectedNode.Quality
         };

    MonitoredNodes.Add(nodeToMonitor);

          System.Diagnostics.Debug.WriteLine($"Started monitoring {SelectedNode.DisplayName}");
       }
            catch (Exception ex)
        {
           System.Diagnostics.Debug.WriteLine($"Error monitoring node: {ex.Message}");
       System.Windows.MessageBox.Show(
                    $"Failed to monitor node: {ex.Message}",
             "Monitor Error",
  System.Windows.MessageBoxButton.OK,
  System.Windows.MessageBoxImage.Error);
       }
        }

        private void UnmonitorNode()
        {
     if (SelectedNode == null)
       return;

         var monitoredNode = MonitoredNodes.FirstOrDefault(n => n.NodeId == SelectedNode.NodeId);
            if (monitoredNode != null)
         {
          MonitoredNodes.Remove(monitoredNode);
                System.Diagnostics.Debug.WriteLine($"Stopped monitoring {SelectedNode.DisplayName}");

    // If no more monitored nodes, remove all from subscription
     if (MonitoredNodes.Count == 0)
           {
   _opcClientService.RemoveAllMonitoredItems();
           IsMonitoring = false;
         }
         }
        }

        private void ClearMonitored()
        {
            MonitoredNodes.Clear();
    _opcClientService.RemoveAllMonitoredItems();
   IsMonitoring = false;
    }

 private bool IsNodeMonitored(OpcNodeInfo node)
        {
    return node != null && MonitoredNodes.Any(n => n.NodeId == node.NodeId);
     }

        private void CopyNodeId()
  {
            if (SelectedNode != null)
   {
            System.Windows.Clipboard.SetText(SelectedNode.NodeId);
System.Diagnostics.Debug.WriteLine($"Copied NodeId: {SelectedNode.NodeId}");
   }
    }

        private void Search()
        {
       // TODO: Implement search functionality
   System.Windows.MessageBox.Show(
                "Search functionality coming soon!",
       "Search",
       System.Windows.MessageBoxButton.OK,
          System.Windows.MessageBoxImage.Information);
        }
    }
}
