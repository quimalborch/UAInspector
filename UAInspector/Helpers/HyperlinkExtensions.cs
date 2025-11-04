using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace UAInspector.Helpers
{
    public static class HyperlinkExtensions
    {
        public static bool GetNavigate(DependencyObject obj)
 {
   return (bool)obj.GetValue(NavigateProperty);
        }

 public static void SetNavigate(DependencyObject obj, bool value)
   {
            obj.SetValue(NavigateProperty, value);
 }

      public static readonly DependencyProperty NavigateProperty =
DependencyProperty.RegisterAttached(
        "Navigate",
     typeof(bool),
        typeof(HyperlinkExtensions),
     new PropertyMetadata(false, OnNavigateChanged));

        private static void OnNavigateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
     {
     if ((bool)e.NewValue)
            {
                if (d is Hyperlink hyperlink)
       {
    hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
        }
            }
     }

        private static void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
 {
      try
    {
 Process.Start(new ProcessStartInfo
       {
            FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
  });
          }
         catch (Exception ex)
            {
       Debug.WriteLine($"Error opening URL: {ex.Message}");
 }
            e.Handled = true;
        }
  }
}
