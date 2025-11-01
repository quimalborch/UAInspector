using System;
using System.Globalization;
using System.Windows.Data;

namespace UAInspector.Helpers
{
    /// <summary>
    /// Converts boolean IsWritable to "R/W" or "R" string
    /// </summary>
    public class BoolToRWConverter : IValueConverter
 {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
   if (value is bool isWritable)
   {
      return isWritable ? "R/W" : "R";
   }
   return "R";
 }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
   {
            throw new NotImplementedException();
 }
    }
}
