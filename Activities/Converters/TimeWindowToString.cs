/*	
Copyright (c) 2015 Microsoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. 
 */
using System;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Data;

namespace ActivitiesExample.Converters
{
    /// <summary>
    /// Helper class to convert time window to string
    /// </summary>
    class TimeWindowToString : IValueConverter
    {
        #region Private members
        /// <summary>
        /// Constructs a new ResourceLoader object
        /// </summary>
        private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView("Resources");
        #endregion

        /// <summary>
        /// Convert activity time window to string
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The type of the target property, as a type reference </param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="language">The language of the conversion.</param>
        /// <returns>The value to be passed to the target dependency property.</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string twString = "";
            if ((double)value == 0)
            {
                twString = this._resourceLoader.GetString("TimeWindow/Today");
            }
            else if ((double)value == -1)
            {
                twString = this._resourceLoader.GetString("TimeWindow/Yesterday");
            }
            else
            {
                var sdatefmt = new Windows.Globalization.DateTimeFormatting.DateTimeFormatter("shortdate");
                twString = sdatefmt.Format(DateTime.Now.Date.AddDays((double)value));
            }
            return twString;
        }

        /// <summary>
        /// Remove time window string
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The type of the target property, as a type reference </param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="language">The language of the conversion.</param>
        /// <returns>The value to be passed to the target dependency property.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string result = "";
            return result;
        }
    }
}
