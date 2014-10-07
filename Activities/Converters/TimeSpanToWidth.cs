using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace ActivitiesExample.Converters
{
    public class TimeSpanToWidth : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // 320 is an ugly hardcoded value for list width (350) - elipsis column (30)
            // would be nice if could be suplied as 'parameter' via a binding to list's ActualWidth
            // but ConverterParameter is non-bindable :(
            return (320 * ((TimeSpan)value).TotalMinutes) / (12*60);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string result = "";

            return result;
        }
    }
}
