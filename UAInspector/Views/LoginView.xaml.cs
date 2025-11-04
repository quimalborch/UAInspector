using System.Windows;
using System.Windows.Controls;
using UAInspector.ViewModels;

namespace UAInspector.Views
{
    public partial class LoginView : UserControl
    {
       public LoginView()
        {
    InitializeComponent();
  this.Loaded += LoginView_Loaded;
     this.Unloaded += LoginView_Unloaded;
       }

  private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
   if (DataContext is LoginViewModel viewModel && sender is PasswordBox pb)
    {
         viewModel.Password = pb.Password;
            }
      }

   private void LoginView_Loaded(object sender, RoutedEventArgs e)
       {
 if (this.FindName("PasswordBox") is PasswordBox passwordBox)
           {
     passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
      }
         if (DataContext is LoginViewModel vm)
  {
       vm.ClearForm();
          }
      }

      private void LoginView_Unloaded(object sender, RoutedEventArgs e)
    {
       if (this.FindName("PasswordBox") is PasswordBox passwordBox)
   {
    passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
    }
        }
    }
}
