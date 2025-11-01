using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UAInspector.Core.Models;
using UAInspector.ViewModels;

namespace UAInspector.Views
{
    /// <summary>
    /// Interaction logic for ServerListView.xaml
    /// </summary>
    public partial class ServerListView : UserControl
    {
        public ServerListView()
        {
            InitializeComponent();
     }

        private void ServerListItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var viewModel = DataContext as ServerListViewModel;
      if (viewModel?.ConnectCommand?.CanExecute(null) == true)
            {
   viewModel.ConnectCommand.Execute(null);
     }
        }

        private void DiscoveredServerItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
     {
            // Get the clicked server from the Border
    var border = sender as FrameworkElement;
 if (border?.DataContext is OpcServerInfo server)
 {
    var viewModel = DataContext as ServerListViewModel;
   if (viewModel != null)
  {
    // Add to saved servers and select it
       viewModel.SelectedServer = server;
       
   // Store it
     var storageService = new UAInspector.Core.Services.StorageService();
      storageService.AddOrUpdateServer(server);
  
 // Refresh the list
    if (viewModel.RefreshCommand?.CanExecute(null) == true)
   {
   viewModel.RefreshCommand.Execute(null);
    }
    
 // Select the newly added server
        viewModel.SelectedServer = server;
       
       // Try to connect
  if (viewModel.ConnectCommand?.CanExecute(null) == true)
      {
       viewModel.ConnectCommand.Execute(null);
   }
    }
 }
        }
    }
}
