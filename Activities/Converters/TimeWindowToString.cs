using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace ActivitiesExample.Converters
{
    class TimeWindowToString : IValueConverter
    {
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string twString = "";

            if ((double)value == 0)
            {
                twString = this.resourceLoader.GetString("TimeWindow/Today");
            }
            else if ((double)value == -1)
            {
                twString = this.resourceLoader.GetString("TimeWindow/Yesterday");
            }
            else
            {
                var sdatefmt = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter("shortdate");
                twString = sdatefmt.Format(DateTime.Now.Date.AddDays((double)value));
            }

            return twString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string result = "";

            return result;
        }
    }
}
