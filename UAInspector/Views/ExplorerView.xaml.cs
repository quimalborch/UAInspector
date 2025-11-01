using System.Windows;
using System.Windows.Controls;
using UAInspector.Core.Models;
using UAInspector.ViewModels;

namespace UAInspector.Views
{
    /// <summary>
 /// Interaction logic for ExplorerView.xaml
    /// </summary>
    public partial class ExplorerView : UserControl
    {
public ExplorerView()
     {
            InitializeComponent();
        }

        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
      {
       var treeViewItem = e.OriginalSource as TreeViewItem;
      if (treeViewItem?.DataContext is OpcNodeInfo node)
            {
                var viewModel = DataContext as ExplorerViewModel;
       if (viewModel != null && node.HasChildren && !node.IsLoaded)
          {
     await viewModel.LoadChildNodesAsync(node);
 }
            }
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
   var treeViewItem = e.OriginalSource as TreeViewItem;
      if (treeViewItem?.DataContext is OpcNodeInfo node)
            {
 var viewModel = DataContext as ExplorerViewModel;
           if (viewModel != null)
         {
       viewModel.SelectedNode = node;
    }
       }
        }
    }
}
