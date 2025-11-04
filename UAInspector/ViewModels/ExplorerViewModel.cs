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
        
        private OpcNodeInfo _selectedFolder;
        private OpcNodeInfo _selectedTag;
        private bool _isLoading;
        private string _writeValue;
      private bool _isSubscribed;

        public ObservableCollection<OpcNodeInfo> FolderTree { get; }
      public ObservableCollection<OpcNodeInfo> TagsInFolder { get; }

        /// <summary>
/// Selected folder in the tree (left panel)
/// </summary>
      public OpcNodeInfo SelectedFolder
        {
  get => _selectedFolder;
 set
       {
    if (SetProperty(ref _selectedFolder, value))
      {
      _ = LoadTagsForFolderAsync(value);
 }
 }
   }

        /// <summary>
        /// Selected tag in the tags list (right panel)
        /// </summary>
 public OpcNodeInfo SelectedTag
        {
 get => _selectedTag;
   set
            {
       if (SetProperty(ref _selectedTag, value))
     {
    OnPropertyChanged(nameof(IsTagSelected));
               OnPropertyChanged(nameof(CanWrite));
         (WriteValueCommand as RelayCommand)?.RaiseCanExecuteChanged();
(CopyNodeIdCommand as RelayCommand)?.RaiseCanExecuteChanged();
  
   // Update write value textbox with current value
   if (value?.Value != null)
  {
        WriteValue = value.Value.ToString();
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

     public bool IsSubscribed
    {
      get => _isSubscribed;
set => SetProperty(ref _isSubscribed, value);
        }

     public bool IsTagSelected => SelectedTag != null;
   public bool CanWrite => SelectedTag?.IsWritable == true;

   public string ServerName => _mainViewModel.CurrentServer?.Name ?? "Unknown";
     public string ServerUrl => _mainViewModel.CurrentServer?.Url ?? "";

    // Commands
     public ICommand RefreshCommand { get; }
  public ICommand WriteValueCommand { get; }
 public ICommand CopyNodeIdCommand { get; }
        public ICommand RefreshTagsCommand { get; }

        public ExplorerViewModel(StorageService storageService, MainViewModel mainViewModel)
   {
_storageService = storageService;
  _mainViewModel = mainViewModel;
            _opcClientService = mainViewModel.OpcClientService;

    FolderTree = new ObservableCollection<OpcNodeInfo>();
    TagsInFolder = new ObservableCollection<OpcNodeInfo>();

  // Initialize commands
    RefreshCommand = new RelayCommand(async () => await LoadRootFoldersAsync());
            WriteValueCommand = new RelayCommand(WriteValueToTag, () => CanWrite && !string.IsNullOrWhiteSpace(WriteValue));
      CopyNodeIdCommand = new RelayCommand(CopyNodeId, () => IsTagSelected);
            RefreshTagsCommand = new RelayCommand(async () => await LoadTagsForFolderAsync(SelectedFolder));

  // Load root folders
   _ = LoadRootFoldersAsync();
        }

      /// <summary>
     /// Load root folders (only Objects, no Variables)
        /// </summary>
   private async Task LoadRootFoldersAsync()
        {
     IsLoading = true;
 try
            {
         FolderTree.Clear();
   TagsInFolder.Clear();

     // Browse from root
      var nodes = await _opcClientService.BrowseAsync(null);

     // Only add folders (Objects), not Variables
                foreach (var node in nodes.Where(n => n.NodeClass == OpcNodeClass.Object || n.NodeClass == OpcNodeClass.ObjectType))
 {
    FolderTree.Add(node);
   }

     System.Diagnostics.Debug.WriteLine($"Loaded {FolderTree.Count} folders");
     }
     catch (Exception ex)
   {
  System.Diagnostics.Debug.WriteLine($"Error loading folders: {ex.Message}");
       System.Windows.MessageBox.Show(
    $"Failed to load folders: {ex.Message}",
      "Error",
        System.Windows.MessageBoxButton.OK,
     System.Windows.MessageBoxImage.Error);
            }
     finally
    {
         IsLoading = false;
}
  }

        /// <summary>
        /// Load child folders for a parent folder (lazy loading)
/// </summary>
        public async Task LoadChildFoldersAsync(OpcNodeInfo parentFolder)
        {
    if (parentFolder == null || parentFolder.IsLoaded)
     return;

       try
 {
     parentFolder.Children.Clear();

          var nodes = await _opcClientService.BrowseAsync(parentFolder.NodeId);

       // Only add folders (Objects), not Variables
      foreach (var node in nodes.Where(n => n.NodeClass == OpcNodeClass.Object || n.NodeClass == OpcNodeClass.ObjectType))
     {
       parentFolder.Children.Add(node);
           }

parentFolder.IsLoaded = true;
     System.Diagnostics.Debug.WriteLine($"Loaded {parentFolder.Children.Count} child folders for {parentFolder.DisplayName}");
        }
      catch (Exception ex)
     {
       System.Diagnostics.Debug.WriteLine($"Error loading child folders: {ex.Message}");
            }
  }

  /// <summary>
 /// Load all tags (variables) for the selected folder and subscribe to them
        /// </summary>
        private async Task LoadTagsForFolderAsync(OpcNodeInfo folder)
{
 if (folder == null)
{
    TagsInFolder.Clear();
 return;
         }

    IsLoading = true;
            try
            {
// Unsubscribe from previous tags
       if (IsSubscribed)
  {
     _opcClientService.RemoveAllMonitoredItems();
       IsSubscribed = false;
}

     TagsInFolder.Clear();

      // Browse the folder to get all children
   var nodes = await _opcClientService.BrowseAsync(folder.NodeId);

 // Only add Variables (tags)
      var tags = nodes.Where(n => n.NodeClass == OpcNodeClass.Variable).ToList();

      foreach (var tag in tags)
  {
       TagsInFolder.Add(tag);
     }

 // If we have tags, create subscription for automatic updates (300ms interval)
   if (TagsInFolder.Count > 0)
    {
     await SubscribeToTagsAsync();
 }

    System.Diagnostics.Debug.WriteLine($"Loaded {TagsInFolder.Count} tags for folder {folder.DisplayName}");
       }
  catch (Exception ex)
    {
 System.Diagnostics.Debug.WriteLine($"Error loading tags: {ex.Message}");
       System.Windows.MessageBox.Show(
         $"Failed to load tags: {ex.Message}",
    "Error",
     System.Windows.MessageBoxButton.OK,
        System.Windows.MessageBoxImage.Error);
      }
   finally
   {
         IsLoading = false;
     }
        }

      /// <summary>
        /// Subscribe to all tags in the current folder for automatic updates every 300ms
        /// </summary>
        private async Task SubscribeToTagsAsync()
        {
try
{
         // Create subscription with 300ms publishing interval
   if (!IsSubscribed)
             {
            await _opcClientService.CreateSubscriptionAsync(300); // 300ms = 0.3 seconds
    }

     // Subscribe to each tag
    foreach (var tag in TagsInFolder)
      {
     var tagNodeId = tag.NodeId; // Capture for closure
            var tagReference = tag; // Capture the actual tag reference
            
   _opcClientService.AddMonitoredItem(tagNodeId, (item, e) =>
         {
       // Update value on notification
     if (e.NotificationValue is MonitoredItemNotification notification)
           {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
         // Update the tag directly - INotifyPropertyChanged will handle UI update
  tagReference.Value = notification.Value.Value;
   tagReference.Timestamp = notification.Value.SourceTimestamp;
                tagReference.Quality = notification.Value.StatusCode.ToString();

    // Also update selected tag if it's the same
        if (SelectedTag?.NodeId == tagNodeId)
     {
            OnPropertyChanged(nameof(SelectedTag));
       }
           });
             }
   });
  }

     IsSubscribed = true;
     System.Diagnostics.Debug.WriteLine($"Subscribed to {TagsInFolder.Count} tags with 300ms update interval");
     }
     catch (Exception ex)
   {
            System.Diagnostics.Debug.WriteLine($"Error subscribing to tags: {ex.Message}");
  }
    }

        /// <summary>
  /// Write value to the selected tag
   /// </summary>
        private async void WriteValueToTag()
    {
      if (!CanWrite || string.IsNullOrWhiteSpace(WriteValue))
     return;

    try
      {
        // Parse value based on data type
 object valueToWrite = ParseValue(WriteValue, SelectedTag.DataType);

   var writeResult = await _opcClientService.WriteValueAsync(SelectedTag.NodeId, valueToWrite);
   
  if (!writeResult.Success)
{
   System.Windows.MessageBox.Show(
  $"Failed to write value to {SelectedTag.DisplayName}:\n\n{writeResult.ErrorMessage}",
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
     if (string.IsNullOrWhiteSpace(dataType) || string.IsNullOrWhiteSpace(value))
    return value;

       try
 {
     // Normalize dataType to lowercase for comparison
            var normalizedType = dataType.ToLowerInvariant();

        if (normalizedType.Contains("boolean"))
    return bool.Parse(value);
  else if (normalizedType.Contains("sbyte"))
        return sbyte.Parse(value);
  else if (normalizedType.Contains("byte") && !normalizedType.Contains("uint"))
     return byte.Parse(value);
  else if (normalizedType.Contains("int16"))
      return short.Parse(value);
  else if (normalizedType.Contains("uint16"))
   return ushort.Parse(value);
  else if (normalizedType.Contains("int32") || (normalizedType.Contains("int") && !normalizedType.Contains("uint") && !normalizedType.Contains("64")))
    return int.Parse(value);
   else if (normalizedType.Contains("uint32") || (normalizedType.Contains("uint") && !normalizedType.Contains("64")))
     return uint.Parse(value);
   else if (normalizedType.Contains("int64"))
    return long.Parse(value);
   else if (normalizedType.Contains("uint64"))
     return ulong.Parse(value);
   else if (normalizedType.Contains("float"))
     return float.Parse(value);
       else if (normalizedType.Contains("double"))
 return double.Parse(value);
        else if (normalizedType.Contains("datetime"))
        return DateTime.Parse(value);
    else if (normalizedType.Contains("guid"))
   return Guid.Parse(value);
   else
       return value;
 }
 catch (Exception ex)
         {
System.Diagnostics.Debug.WriteLine($"Error parsing value '{value}' as type '{dataType}': {ex.Message}");
   return value;
     }
        }

 private void CopyNodeId()
    {
    if (SelectedTag != null)
          {
   System.Windows.Clipboard.SetText(SelectedTag.NodeId);
        System.Diagnostics.Debug.WriteLine($"Copied NodeId: {SelectedTag.NodeId}");
 }
        }
 }
}
