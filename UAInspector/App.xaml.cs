using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using UAInspector.Core.Services;
using Velopack;

namespace UAInspector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [STAThread]
        private static void Main(string[] args)
        {
            VelopackApp.Build().Run();
            App app = new();
            app.InitializeComponent();
            app.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Load and apply saved theme on startup
            try
            {
                var storageService = new StorageService();
                var settings = storageService.LoadSettings();
                var themeService = new ThemeService();

                themeService.ApplyTheme(settings.DarkMode);

                System.Diagnostics.Debug.WriteLine($"Theme applied on startup: {(settings.DarkMode ? "Dark" : "Light")} mode");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme on startup: {ex.Message}");
                // If there's an error, the default theme from App.xaml will be used (Dark)
            }
        }
    }
}
