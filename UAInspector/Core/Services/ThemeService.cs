using System;
using System.Windows;

namespace UAInspector.Core.Services
{
    /// <summary>
    /// Service for managing application themes (Light/Dark mode)
    /// </summary>
    public class ThemeService
    {
        private const string LightThemeUri = "Resources/LightTheme.xaml";
        private const string DarkThemeUri = "Resources/DarkTheme.xaml";
     private const int ThemeResourceIndex = 0; // Position in MergedDictionaries

        /// <summary>
        /// Apply theme based on dark mode setting
        /// </summary>
  public void ApplyTheme(bool isDarkMode)
        {
    try
      {
      var themeUri = isDarkMode ? DarkThemeUri : LightThemeUri;
       var newTheme = new ResourceDictionary
                {
  Source = new Uri(themeUri, UriKind.Relative)
   };

            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

                // Remove old theme if exists
       if (mergedDictionaries.Count > ThemeResourceIndex)
     {
mergedDictionaries.RemoveAt(ThemeResourceIndex);
     }

       // Insert new theme at the beginning
  mergedDictionaries.Insert(ThemeResourceIndex, newTheme);

        System.Diagnostics.Debug.WriteLine($"Theme applied: {(isDarkMode ? "Dark" : "Light")} mode");
   }
    catch (Exception ex)
            {
    System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
        }
        }

        /// <summary>
        /// Get current theme name
        /// </summary>
        public string GetCurrentTheme(bool isDarkMode)
        {
      return isDarkMode ? "Dark" : "Light";
        }
 }
}
