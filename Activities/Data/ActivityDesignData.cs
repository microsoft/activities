using Lumia.Sense;
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ActivitiesExample.Data
{
    /// <summary>
    /// Data class for design mode
    /// </summary>
    public class ActivityDesignData : INotifyPropertyChanged
    {
        #region Private members
        /// <summary>
        /// List of activities and durations
        /// </summary>
        private List<ActivityDuration> _listData = null;

        /// <summary>
        /// Singleton instance
        /// </summary>
        private static ActivityDesignData _selfData;
        #endregion

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This method is called by the Set accessor of each property. 
        /// The CallerMemberName attribute that is applied to the optional propertyName 
        /// parameter causes the property name of the caller to be substituted as an argument
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        private void NotifyPropertyChanged( [CallerMemberName] String propertyName = "" )
        {
            if( PropertyChanged != null )
            {
                PropertyChanged( this, new PropertyChangedEventArgs( propertyName ) );
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityDesignData()
        {
            _listData = new List<ActivityDuration>();
            if( Windows.ApplicationModel.DesignMode.DesignModeEnabled )
            {
                _listData.Add( new ActivityDuration( Activity.Idle, TimeSpan.FromHours( 13 ) ) );
                _listData.Add( new ActivityDuration( Activity.Moving, TimeSpan.FromHours( 4 ) ) );
                _listData.Add( new ActivityDuration( Activity.Stationary, TimeSpan.FromHours( 1 ) ) );
                _listData.Add( new ActivityDuration( Activity.Walking, TimeSpan.FromHours( 2 ) ) );
                _listData.Add( new ActivityDuration( Activity.Running, TimeSpan.FromHours( 3 ) ) );
                _listData.Add( new ActivityDuration( Activity.Biking, TimeSpan.FromHours( 5 ) ) );
                _listData.Add( new ActivityDuration( Activity.MovingInVehicle, TimeSpan.FromHours( 1 ) ) );
                _listData.Add( new ActivityDuration( Activity.Unknown, TimeSpan.FromHours( 1 ) ) );
            }
        }

        /// <summary>
        /// Create new instance of the class
        /// </summary>
        /// <returns>Design data instance</returns>
        static public ActivityDesignData Instance()
        {
            if( _selfData == null )
            {
                _selfData = new ActivityDesignData();
            }
            return _selfData;
        }

        /// <summary>
        /// Get the current activity
        /// </summary>
        public Activity CurrentActivity
        {
            get
            {
                return Activity.Walking;
            }
        }

        /// <summary>
        /// Get the list of activities and durations 
        /// </summary>
        public List<ActivityDuration> History
        {
            get
            {
                return _listData;
            }
        }

        /// <summary>
        /// Date of the data
        /// </summary>
        public DateTime Date 
        {
            get { return DateTime.Today; } 
        }
    }
}

// end of file
