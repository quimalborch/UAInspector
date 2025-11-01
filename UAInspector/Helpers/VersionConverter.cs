using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace UAInspector.Helpers
{
    /// <summary>
    /// Converter that returns the application version from the assembly
    /// </summary>
    public class VersionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                // Get the version from the executing assembly
                var version = Assembly.GetExecutingAssembly().GetName().Version;
        
                // Format: v1.0.0 (without build number)
                return $"v{version.Major}.{version.Minor}.{version.Build}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting version: {ex.Message}");
                return "v1.0.0";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
