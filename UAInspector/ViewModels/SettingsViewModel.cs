using System;
using System.Windows.Input;
using UAInspector.Core.Models;
using UAInspector.Core.Services;
using UAInspector.Helpers;

namespace UAInspector.ViewModels
{
    /// <summary>
    /// ViewModel for application settings
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        private readonly StorageService _storageService;
        private readonly MainViewModel _mainViewModel;
        private readonly ThemeService _themeService;
        private AppSettings _settings;
        private bool _startWithWindows;
        private bool _isDirty;

        /// <summary>
        /// Application settings
        /// </summary>
        public AppSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        /// <summary>
        /// Start with Windows setting
        /// </summary>
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (SetProperty(ref _startWithWindows, value))
                {
                    IsDirty = true;
                    ApplyStartWithWindows(value);
                }
            }
        }

        /// <summary>
        /// Dark mode setting - inversed for UI (false = Dark, true = Light)
        /// </summary>
        public bool IsLightMode
        {
            get => !Settings?.DarkMode ?? false;
            set
            {
                if (Settings != null && Settings.DarkMode == value)
                {
                    Settings.DarkMode = !value;
                    OnPropertyChanged(nameof(IsLightMode));
                    OnPropertyChanged(nameof(ThemeDisplayName));
                    IsDirty = true;
                    ApplyTheme(!value); // Apply immediately

                    // Auto-save theme changes
                    SaveThemeSetting();
                }
            }
        }

        /// <summary>
        /// Display name for current theme
        /// </summary>
        public string ThemeDisplayName => IsLightMode ? "Light Mode" : "Dark Mode";

        /// <summary>
        /// Indicates if settings have been modified
        /// </summary>
        public bool IsDirty
        {
            get => _isDirty;
            set => SetProperty(ref _isDirty, value);
        }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand ExportCommand { get; }

        public SettingsViewModel(StorageService storageService, MainViewModel mainViewModel)
        {
            _storageService = storageService;
            _mainViewModel = mainViewModel;
            _themeService = new ThemeService();

            // Load settings
            LoadSettings();

            // Initialize commands
            SaveCommand = new RelayCommand(SaveSettings, () => IsDirty);
            ResetCommand = new RelayCommand(ResetSettings);
            ExportCommand = new RelayCommand(ExportSettings);
        }

        /// <summary>
        /// Load settings from storage and registry
        /// </summary>
        private void LoadSettings()
        {
            Settings = _storageService.LoadSettings();

            // Load StartWithWindows from registry
            _startWithWindows = StartupHelper.IsStartupEnabled();
            OnPropertyChanged(nameof(StartWithWindows));

            // Sync with settings file
            if (Settings.StartWithWindows != _startWithWindows)
            {
                Settings.StartWithWindows = _startWithWindows;
                _storageService.SaveSettings(Settings);
            }

            // Apply saved theme
            ApplyTheme(Settings.DarkMode);
            OnPropertyChanged(nameof(IsLightMode));
            OnPropertyChanged(nameof(ThemeDisplayName));

            IsDirty = false;
            System.Diagnostics.Debug.WriteLine($"Settings loaded - StartWithWindows: {StartWithWindows}, Theme: {ThemeDisplayName}");
        }

        /// <summary>
        /// Save settings to storage
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                Settings.LastUpdated = DateTime.Now;
                _storageService.SaveSettings(Settings);

                // Update MainViewModel settings
                _mainViewModel.Settings = Settings;

                IsDirty = false;

                System.Windows.MessageBox.Show(
                    "Settings saved successfully!",
                    "Settings",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                System.Diagnostics.Debug.WriteLine("Settings saved successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Failed to save settings: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Reset settings to default values
        /// </summary>
        private void ResetSettings()
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to reset all settings to default values?\n\nThis cannot be undone.",
                "Confirm Reset",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                Settings = new AppSettings();
                StartWithWindows = false;
                ApplyTheme(Settings.DarkMode);
                OnPropertyChanged(nameof(IsLightMode));
                OnPropertyChanged(nameof(ThemeDisplayName));
                IsDirty = true;
                SaveSettings();

                System.Diagnostics.Debug.WriteLine("Settings reset to defaults");
            }
        }

        /// <summary>
        /// Export settings to file
        /// </summary>
        private void ExportSettings()
        {
            // TODO: Implement export to user-selected file
            System.Windows.MessageBox.Show(
                "Export functionality coming soon!\n\nThis will allow you to export settings to a backup file.",
                "Export Settings",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }

        /// <summary>
        /// Apply StartWithWindows setting to Windows registry
        /// </summary>
        private void ApplyStartWithWindows(bool enable)
        {
            try
            {
                bool success = StartupHelper.SetStartup(enable);

                if (success)
                {
                    Settings.StartWithWindows = enable;
                    System.Diagnostics.Debug.WriteLine($"StartWithWindows set to: {enable}");
                }
                else
                {
                    // Revert on failure
                    _startWithWindows = !enable;
                    OnPropertyChanged(nameof(StartWithWindows));

                    System.Windows.MessageBox.Show(
                        "Failed to update Windows startup settings.\n\nMake sure you have the necessary permissions.",
                        "Startup Error",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying StartWithWindows: {ex.Message}");

                // Revert on error
                _startWithWindows = !enable;
                OnPropertyChanged(nameof(StartWithWindows));

                System.Windows.MessageBox.Show(
                    $"Error updating startup settings: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Apply theme to application
        /// </summary>
        private void ApplyTheme(bool isDarkMode)
        {
            try
            {
                _themeService.ApplyTheme(isDarkMode);
                System.Diagnostics.Debug.WriteLine($"Theme applied: {(isDarkMode ? "Dark" : "Light")} mode");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
            }
        }

        /// <summary>
        /// Save only theme setting without showing confirmation message
        /// </summary>
        private void SaveThemeSetting()
        {
            try
            {
                Settings.LastUpdated = DateTime.Now;
                _storageService.SaveSettings(Settings);

                // Update MainViewModel settings
                _mainViewModel.Settings = Settings;

                // Reset IsDirty since theme is auto-saved
                IsDirty = false;

                System.Diagnostics.Debug.WriteLine($"Theme setting auto-saved: {ThemeDisplayName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error auto-saving theme setting: {ex.Message}");
            }
        }
    }
}
