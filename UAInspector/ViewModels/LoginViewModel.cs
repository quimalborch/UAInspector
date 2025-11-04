using System;
using System.Windows.Input;
using UAInspector.Core.Models;

namespace UAInspector.ViewModels
{
    /// <summary>
    /// ViewModel for login dialog
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        private string _username;
        private string _password;
        private bool _rememberCredentials;
   private string _errorMessage;
        private bool _isLoading;
        private LoginType _selectedLoginType;
        private bool _isAnonymousMode;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
   }

     public bool RememberCredentials
        {
        get => _rememberCredentials;
            set => SetProperty(ref _rememberCredentials, value);
        }

 public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoading
        {
        get => _isLoading;
    set => SetProperty(ref _isLoading, value);
        }

 public LoginType SelectedLoginType
        {
            get => _selectedLoginType;
            set
            {
            if (SetProperty(ref _selectedLoginType, value))
          {
               IsAnonymousMode = (value == LoginType.Anonymous);
      OnPropertyChanged(nameof(IsCredentialsVisible));
    }
            }
        }

      public bool IsAnonymousMode
        {
          get => _isAnonymousMode;
 set => SetProperty(ref _isAnonymousMode, value);
        }

        public bool IsCredentialsVisible => !IsAnonymousMode;

        public ICommand LoginCommand { get; }
     public ICommand CancelCommand { get; }

        // Events
  public event EventHandler<LoginResultEventArgs> LoginRequested;
  public event EventHandler CancelRequested;

        public LoginViewModel()
  {
   SelectedLoginType = LoginType.Anonymous;
        RememberCredentials = false;
            
  LoginCommand = new RelayCommand(ExecuteLogin, CanLogin);
       CancelCommand = new RelayCommand(ExecuteCancel);
  }

        private bool CanLogin()
        {
 if (IsAnonymousMode)
     return !IsLoading;
            
        return !IsLoading && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }

  private void ExecuteLogin()
    {
          ErrorMessage = string.Empty;
            
     if (IsAnonymousMode)
{
        LoginRequested?.Invoke(this, new LoginResultEventArgs
           {
  LoginType = LoginType.Anonymous,
 Username = null,
    Password = null,
   RememberCredentials = false,
            Success = true
});
            }
   else
            {
                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
         {
          ErrorMessage = "Please enter username and password";
                 return;
        }

                LoginRequested?.Invoke(this, new LoginResultEventArgs
       {
      LoginType = SelectedLoginType,
          Username = Username,
         Password = Password,
           RememberCredentials = RememberCredentials,
        Success = true
          });
     }
     }

   private void ExecuteCancel()
        {
   CancelRequested?.Invoke(this, EventArgs.Empty);
    }

        public void SetError(string message)
        {
            ErrorMessage = message;
IsLoading = false;
        }

      public void ClearForm()
    {
      Username = string.Empty;
         Password = string.Empty;
 ErrorMessage = string.Empty;
     IsLoading = false;
        }
    }

    public class LoginResultEventArgs : EventArgs
    {
        public LoginType LoginType { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberCredentials { get; set; }
        public bool Success { get; set; }
    }
}
