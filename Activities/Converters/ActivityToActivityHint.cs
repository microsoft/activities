using Lumia.Sense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace ActivitiesExample.Converters
{
    class ActivityToActivityHint : IValueConverter
    {
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string hint = "";

            switch(((string)value).ToLower())
            {
                case "moving":
                    hint = this.resourceLoader.GetString("Hint/Moving");
                    break;
                case "idle":
                    hint = this.resourceLoader.GetString("Hint/Idle");
                    break;
                case "stationary":
                    hint = this.resourceLoader.GetString("Hint/Stationary");
                    break;
                case "walking":
                    hint = this.resourceLoader.GetString("Hint/Walking");
                    break;
                case "running":
                    hint = this.resourceLoader.GetString("Hint/Running");
                    break;
                default: break;
            }
               
            return hint;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string result = "";

            return result;
        }
    }
}
