using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace UAInspector.Helpers
{
    /// <summary>
    /// Helper class to manage Windows startup registry entry
    /// </summary>
    public static class StartupHelper
    {
        private const string AppName = "UAInspector";
        private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
      /// Get the path to the executable
        /// </summary>
        private static string GetExecutablePath()
  {
 return Process.GetCurrentProcess().MainModule.FileName;
        }

        /// <summary>
        /// Check if the application is set to start with Windows
        /// </summary>
        public static bool IsStartupEnabled()
        {
       try
      {
       using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
   {
      if (key == null)
        return false;

 var value = key.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
     }
}
        catch (Exception ex)
            {
                Debug.WriteLine($"Error checking startup status: {ex.Message}");
                return false;
   }
        }

  /// <summary>
        /// Enable or disable startup with Windows
        /// </summary>
        public static bool SetStartup(bool enable)
        {
       try
       {
          using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
    {
        if (key == null)
      {
             Debug.WriteLine("Unable to open registry key");
             return false;
       }

   if (enable)
          {
  // Add to startup
      string exePath = GetExecutablePath();
                    key.SetValue(AppName, $"\"{exePath}\"");
 Debug.WriteLine($"Added {AppName} to Windows startup: {exePath}");
              return true;
           }
          else
       {
       // Remove from startup
      if (key.GetValue(AppName) != null)
             {
    key.DeleteValue(AppName);
  Debug.WriteLine($"Removed {AppName} from Windows startup");
            }
           return true;
        }
       }
 }
            catch (Exception ex)
            {
      Debug.WriteLine($"Error setting startup: {ex.Message}");
      return false;
            }
  }

      /// <summary>
        /// Verify if the current registry entry matches the current executable path
        /// </summary>
        public static bool VerifyStartupPath()
   {
     try
 {
       using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
 {
          if (key == null)
      return false;

        var registryValue = key.GetValue(AppName) as string;
     if (string.IsNullOrEmpty(registryValue))
    return false;

        // Remove quotes if present
    var registryPath = registryValue.Trim('"');
              var currentPath = GetExecutablePath();

        return string.Equals(registryPath, currentPath, StringComparison.OrdinalIgnoreCase);
            }
            }
            catch (Exception ex)
            {
     Debug.WriteLine($"Error verifying startup path: {ex.Message}");
   return false;
      }
    }

        /// <summary>
/// Update the registry entry if the executable path has changed
     /// </summary>
    public static bool UpdateStartupPath()
        {
  if (!IsStartupEnabled())
 return true;

            if (VerifyStartupPath())
          return true;

            // Path has changed, update it
    return SetStartup(true);
        }
    }
}
