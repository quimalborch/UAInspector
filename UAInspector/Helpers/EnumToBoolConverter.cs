using System;
using System.Globalization;
using System.Windows.Data;

namespace UAInspector.Helpers
{
 /// <summary>
    /// Converts enum value to boolean for RadioButton binding
    /// </summary>
    public class EnumToBoolConverter : IValueConverter
    {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
     if (parameter == null || value == null)
    return false;

            return value.ToString().Equals(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  {
 if (value is bool isSelected && isSelected && parameter != null)
          {
          return Enum.Parse(targetType, parameter.ToString());
      }
        return Binding.DoNothing;
        }
    }
}
