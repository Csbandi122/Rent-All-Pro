﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace RentAllPro.Converters
{
    public class BoolToYesNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Igen" : "Nem";
            }
            return "Nem";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return stringValue.Equals("Igen", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}