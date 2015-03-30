/*	
The MIT License (MIT)
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
    /// <summary>
    /// Helper class to convert activity to activity hint
    /// </summary>
    class ActivityToActivityHint : IValueConverter
    {
        #region Private members
        /// <summary>
        /// Constructs a new ResourceLoader object
        /// </summary>
        private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView( "Resources" );
        #endregion

        /// <summary>
        /// Get activity description 
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The type of the target property, as a type reference </param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="language">The language of the conversion.</param>
        /// <returns>The value to be passed to the target dependency property.</returns>
        public object Convert( object value, Type targetType, object parameter, string language )
        {
            string hint = "";
            switch( (Activity)value )
            {
                case Activity.Moving:
                    hint = this._resourceLoader.GetString( "Hint/Moving" );
                    break;
                case Activity.Idle:
                    hint = this._resourceLoader.GetString( "Hint/Idle" );
                    break;
                case Activity.Stationary:
                    hint = this._resourceLoader.GetString( "Hint/Stationary" );
                    break;
                case Activity.Walking:
                    hint = this._resourceLoader.GetString( "Hint/Walking" );
                    break;
                case Activity.Running:
                    hint = this._resourceLoader.GetString( "Hint/Running" );
                    break;
                case Activity.Biking:
                    hint = this._resourceLoader.GetString( "Hint/Biking" );
                    break;
                case Activity.MovingInVehicle:
                    hint = this._resourceLoader.GetString( "Hint/MovingInVehicle" );
                    break;
                case Activity.Unknown:
                    hint = this._resourceLoader.GetString( "Hint/Unknown" );
                    break;
                default:
                    break;
            }
            return hint;
        }

        /// <summary>
        /// Remove activity description 
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The type of the target property, as a type reference </param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="language">The language of the conversion.</param>
        /// <returns>The value to be passed to the target dependency property.</returns>
        public object ConvertBack( object value, Type targetType, object parameter, string language )
        {
            string result = "";
            return result;
        }
    }
}