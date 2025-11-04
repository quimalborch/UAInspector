using System;
using System.Globalization;
using System.Windows.Data;

namespace UAInspector.Helpers
{
 /// <summary>
  /// Converts multiple boolean values using AND logic
    /// </summary>
    [ValueConversion(typeof(bool[]), typeof(bool))]
    public class BooleanAndConverter : IMultiValueConverter
    {
   public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
   if (values == null || values.Length == 0)
                return true;

        foreach (object value in values)
     {
         if (value is bool && !(bool)value)
           {
            return false;
     }
    }
           return true;
       }

   public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
 {
      throw new NotImplementedException();
   }
    }
}
