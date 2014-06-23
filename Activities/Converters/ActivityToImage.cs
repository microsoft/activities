using Lumia.Sense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace ActivitiesExample.Converters
{
    class ActivityToImage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Uri uri = new Uri("ms-appx:///Assets/Activities/" + ((string)value).ToLower() + ".png", UriKind.Absolute);
            return uri;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string result = "";

            return result;
        }
    }
}
