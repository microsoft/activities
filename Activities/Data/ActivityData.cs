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
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ActivitiesExample.Data
{
    /// <summary>
    /// Data class for storing activity data for displaying in UI
    /// </summary>
    public class ActivityData : INotifyPropertyChanged
    {
        #region Private members
        /// <summary>
        /// List of activities and durations
        /// </summary>
        private List<ActivityDuration> _listData = null;

        /// <summary>
        /// Current activity
        /// </summary>
        private Activity _currentActivity = Activity.Idle;

        /// <summary>
        /// Date of the data set
        /// </summary>
        private DateTime _date = DateTime.Today;

        /// <summary>
        /// Singleton instance
        /// </summary>
        private static ActivityData _selfData;
        #endregion

        #region Events
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        /// <summary>
        /// This method is called by the Set accessor of each property. 
        /// The CallerMemberName attribute that is applied to the optional propertyName 
        /// parameter causes the property name of the caller to be substituted as an argument.
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
        private ActivityData()
        {
            _listData = new List<ActivityDuration>();
        }

        /// <summary>
        /// Create new instance of the class
        /// </summary>
        /// <returns>Data instance</returns>
        static public ActivityData Instance()
        {
            if( _selfData == null )
            {
                _selfData = new ActivityData();
            }
            return _selfData;
        }

        /// <summary>
        /// Date of the data
        /// </summary>
        public DateTime Date 
        {
            get
            {
                return _date;
            }
            set
            {
                _date = value;
                NotifyPropertyChanged( "Date" );
            }
        }

        /// <summary>
        /// Current activity
        /// </summary>
        public Activity CurrentActivity 
        {
            get
            {
                return _currentActivity;
            }
            set
            {
                _currentActivity = value;
                NotifyPropertyChanged( "CurrentActivity" );
            }
        }

        /// <summary>
        /// Summary of activities for a day
        /// </summary>
        public List<ActivityDuration> History
        {
            get
            {
                return _listData;
            }
            set
            {
                _listData = value;
                NotifyPropertyChanged( "History" );
            }
        }
    }

    /// <summary>
    /// Class containing activity type and duration
    /// </summary>
    public class ActivityDuration
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Activity type</param>
        /// <param name="duration">Activity duration</param>
        public ActivityDuration( Activity type, TimeSpan duration )
        {
            // Split activity string by capital letter
            Duration = duration;
            Type = type;
        }

        /// <summary>
        /// Activity name
        /// </summary>
        public String Name
        {
            get
            {
                return System.Text.RegularExpressions.Regex.Replace( Type.ToString(), @"([A-Z])(?<=[a-z]\1|[A-Za-z]\1(?=[a-z]))", " $1" );
            }
        }

        /// <summary>
        /// Activity type 
        /// </summary>
        public Activity Type { get; set; }

        /// <summary>
        /// Activity duration
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}

// end of file
