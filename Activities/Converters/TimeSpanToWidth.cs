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
            return (352 * ((TimeSpan)value).TotalMinutes) / (12*60);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string result = "";

            return result;
        }
    }
}
