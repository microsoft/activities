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
using Lumia.Sense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ActivitiesExample.Data
{
    /// <summary>
    /// Data class for getting users activities 
    /// </summary>
    public class MyData : INotifyPropertyChanged
    {
        #region Private members
        /// <summary>
        /// List of activities and durations
        /// </summary>
        private List<MyQuantifiedData> _listData = null;

        /// <summary>
        /// Data instance
        /// </summary>
        private static MyData _selfData;

        /// <summary>
        /// List of history data
        /// </summary>
        private IList<ActivityMonitorReading> _historyData;

        /// <summary>
        /// Activity instance
        /// </summary>
        private Activity _activityMode = Activity.Idle;

        /// <summary>
        /// Time window index, 0 = today, -1 = yesterday 
        /// </summary>      
        private double _timeWindowIndex = 0;
        #endregion

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This method is called by the Set accessor of each property. 
        /// The CallerMemberName attribute that is applied to the optional propertyName 
        /// parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        /// <param name="propertyName"></param>
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public MyData()
        {
            _listData = new List<MyQuantifiedData>();
        }

        /// <summary>
        /// Create new instance of the class
        /// </summary>
        /// <returns>Data instance</returns>
        static public MyData Instance()
        {
            if (_selfData == null)
                _selfData = new MyData();
            return _selfData;
        }

        /// <summary>
        /// Get the current activity
        /// </summary>
        public string CurrentActivity
        {
            get
            {
                return _activityMode.ToString().ToLower();
            }
        }

        /// <summary>
        /// Set the current activity
        /// </summary>
        public Activity ActivityEnum
        {
            set
            {
                _activityMode = value;
                NotifyPropertyChanged("CurrentActivity");
            }
        }

        /// <summary>
        /// Get the time window
        /// </summary>
        public double TimeWindow
        {
            get
            {
                return _timeWindowIndex;
            }
        }

        /// <summary>
        /// Set the time window to today
        /// </summary>
        public void NextDay()
        {
            if (_timeWindowIndex < 0)
            {
                _timeWindowIndex++;
                NotifyPropertyChanged("TimeWindow");
            }
        }


        /// <summary>
        /// Set the time window to previous day
        /// </summary>
        public void PreviousDay()
        {
            if (_timeWindowIndex >= -9)
            {
                _timeWindowIndex--;
                NotifyPropertyChanged("TimeWindow");
            }
        }


        /// <summary>
        /// List of activities occured during given time period.
        /// </summary>
        public IList<ActivityMonitorReading> History
        {
            get
            {
                return _historyData;
            }
            set
            {
                if (_historyData == null)
                {
                    _historyData = new List<ActivityMonitorReading>();
                }
                else
                {
                    _historyData.Clear();
                }
                _historyData = value;
                QuantifyData();
            }
        }

        /// <summary>
        /// Get the list of activities and durations 
        /// </summary>
        public List<MyQuantifiedData> ListData
        {
            get
            {
                return _listData;
            }
        }

        /// <summary>
        /// Populate the list of activities and durations to display in the UI 
        /// </summary>
        private void QuantifyData()
        {
            if (_listData != null)
            {
                _listData.Clear();
            }
            _listData = new List<MyQuantifiedData>();
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                List<string> _activitiesList = new List<string>(Enum.GetNames(typeof(Activity)));
                Dictionary<Activity, int> indexer = new Dictionary<Activity, int>();
                TimeSpan[] _durations = new TimeSpan[_activitiesList.Count];
                Activity[] values = (Activity[])Enum.GetValues(typeof(Activity));
                for (int i = 0; i < values.Length; i++)
                {
                    indexer.Add(values[i], i);
                }
                // There could be days with no data (e.g. of phone was turned off)
                if (_historyData.Count > 0)
                {
                    // First entry may be from previous time window, is there any data from current time window?
                    bool hasDataInTimeWindow = false;

                    // Insert new fist entry, representing the last activity of the previous time window
                    // this helps capture that activity's duration but only from the start of current time window                    
                    ActivityMonitorReading first = _historyData[0];
                    if (first.Timestamp <= DateTime.Now.Date.AddDays(_timeWindowIndex))
                    {
                        // Create new "first" entry, with the same mode but timestamp set as 0:00h in current time window
                        _historyData.Insert(1, new ActivityMonitorReading(first.Mode, DateTime.Now.Date.AddDays(_timeWindowIndex)));
                        // Remove previous entry
                        _historyData.RemoveAt(0);
                        hasDataInTimeWindow = _historyData.Count > 1;
                    }
                    else
                    {
                        // The first entry belongs to the current time window
                        // there is no known activity before it
                        hasDataInTimeWindow = true;
                    }
                    // If at least one activity is recorded in this time window
                    if (hasDataInTimeWindow)
                    {
                        // Insert a last activity, marking the begining of the next time window
                        // this helps capturing the correct duration of the last activity stated in this time window
                        ActivityMonitorReading last = _historyData.Last();
                        if (last.Timestamp < DateTime.Now.Date.AddDays(_timeWindowIndex + 1))
                        {
                            // Is this today's time window
                            if (_timeWindowIndex == 0)
                            {
                                // Last activity duration measured until this instant time
                                _historyData.Add(new ActivityMonitorReading(last.Mode, DateTime.Now));
                            }
                            else
                            {
                                // Last activity measured until the begining of the next time index
                                _historyData.Add(new ActivityMonitorReading(last.Mode, DateTime.Now.Date.AddDays(_timeWindowIndex + 1)));
                            }
                        }
                        // Calculate duration for each current activity by subtracting its timestamp from that of the next one
                        for (int i = 0; i < _historyData.Count - 1; i++)
                        {
                            ActivityMonitorReading current = _historyData[i];
                            ActivityMonitorReading next = _historyData[i + 1];
                            _durations[indexer[current.Mode]] += next.Timestamp - current.Timestamp;
                        }
                    }
                }
                // Populate the list to be displayed in the UI
                for (int i = 0; i < _activitiesList.Count; i++)
                {
                    _listData.Add(new MyQuantifiedData(_activitiesList[i], _durations[i]));
                }
            }
            NotifyPropertyChanged("ListData");
        }
    }

    /// <summary>
    ///  Helper class to create a list of activities and their timestamp 
    /// </summary>
    public class MyQuantifiedData
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public MyQuantifiedData(string s, TimeSpan i)
        {
            ActivityName = s;
            ActivityTime = i;
        }

        /// <summary>
        /// Activity name 
        /// </summary>
        public string ActivityName
        {
            get;
            set;
        }

        /// <summary>
        /// Activity time
        /// </summary>
        public TimeSpan ActivityTime
        {
            get;
            set;
        }
    }
}
