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

        /// <summary>
        /// When a folder is expanded, load its child folders (lazy loading)
        /// </summary>
        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem?.DataContext is OpcNodeInfo folder)
            {
                var viewModel = DataContext as ExplorerViewModel;
                if (viewModel != null && folder.HasChildren && !folder.IsLoaded)
                {
                    await viewModel.LoadChildFoldersAsync(folder);
                }
            }
        }

        /// <summary>
        /// When a folder is selected, update SelectedFolder which triggers tag loading
        /// </summary>
        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            var treeViewItem = e.OriginalSource as TreeViewItem;
            if (treeViewItem?.DataContext is OpcNodeInfo folder)
            {
                var viewModel = DataContext as ExplorerViewModel;
                if (viewModel != null)
                {
                    viewModel.SelectedFolder = folder;
                }
            }
        }
    }
}
