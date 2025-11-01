using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UAInspector.ViewModels;
using Velopack;
using Velopack.Sources;

namespace UAInspector.Core.Services
{
    /// <summary>
    /// Service for managing application updates using Velopack
    /// </summary>
    public class UpdateService
    {
        private readonly UpdateManager _updateManager;
        private UpdateInfo _pendingUpdate;
        private MainViewModel _mainViewModel;

        public UpdateService(MainViewModel mainViewModel)
        {
            try
            {
                _mainViewModel = mainViewModel;
                _updateManager = new UpdateManager(new GithubSource("https://github.com/quimalborch/UAInspector", null, false));
                Debug.WriteLine("UpdateManager initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to initialize UpdateManager: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if updates are available
        /// </summary>
        public async Task<bool> CheckForUpdatesAsync()
        {
            try
            {
                if (_updateManager == null)
                {
                    Debug.WriteLine("UpdateManager is null, cannot check for updates");
                    return false;
                }

                Debug.WriteLine("Checking for updates...");
                _pendingUpdate = await _updateManager.CheckForUpdatesAsync().ConfigureAwait(true);

                if (_pendingUpdate == null)
                {
                    Debug.WriteLine("No updates available");
                    return false;
                }

                Debug.WriteLine($"Update available: {_pendingUpdate.TargetFullRelease.Version}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking for updates: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Download and apply updates, then restart the application
        /// </summary>
        public async Task DownloadAndApplyUpdatesAsync()
        {
            try
            {
                if (_updateManager == null || _pendingUpdate == null)
                {
                    Debug.WriteLine("Cannot apply updates: UpdateManager or pending update is null");
                    return;
                }

                Debug.WriteLine("Downloading updates...");
                await _updateManager.DownloadUpdatesAsync(_pendingUpdate, Progress);

                Debug.WriteLine("Applying updates and restarting...");
                _updateManager.ApplyUpdatesAndRestart(_pendingUpdate);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying updates: {ex.Message}");
                throw;
            }
        }

        private void Progress(int percent)
        {
            // progress can be sent from other threads
            _mainViewModel.StatusMessage = $"Downloading update... {percent}%";
        }

        /// <summary>
        /// Get the version string of the pending update
        /// </summary>
        public string GetPendingUpdateVersion()
        {
            return _pendingUpdate?.TargetFullRelease.Version.ToString() ?? "Unknown";
        }
    }
}
