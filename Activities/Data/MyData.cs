using Lumia.Sense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;


namespace ActivitiesExample.Data
{
    public class MyData : INotifyPropertyChanged
    {
        private List<MyQuantifiedData> _ListData = null;

        private static MyData _self;

        private IList<ActivityMonitorReading> _history;
        private Activity _activity = Activity.Idle;

        // time window index, 0 = today, -1 = yesterday 
        private double timeWindowIndex = 0;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public MyData()
        {
            _ListData = new List<MyQuantifiedData>();
        }

        static public MyData Instance()
        {
            if (_self == null)
                _self = new MyData();
            return _self;
        }

        public string CurrentActivity
        {
            get
            {
               return _activity.ToString().ToLower();
            }
        }

        public Activity ActivityEnum
        {
            set
            {
                _activity = value;
                NotifyPropertyChanged("CurrentActivity");
            }
        }

        public double TimeWindow
        {
            get
            {
                return timeWindowIndex;
            }
        }

        public void NextDay()
        {
            if (timeWindowIndex < 0)
            {
                timeWindowIndex++;
                NotifyPropertyChanged("TimeWindow");
            }
        }

        public void PreviousDay()
        {
            if (timeWindowIndex >= -9)
            {
                timeWindowIndex--;
                NotifyPropertyChanged("TimeWindow");
            }
        }

        public IList<ActivityMonitorReading> History
        {
            get 
            {
                return _history;
            }
            set
            {
                if (_history == null)
                {
                    _history = new List<ActivityMonitorReading>();
                }
                else
                {
                    _history.Clear();
                }

                _history = value;
                QuantifyData();
            }
        }

        public List<MyQuantifiedData> ListData
        {
            get
            {
                return _ListData;
            }
        }

        private void QuantifyData()
        {
            if (_ListData != null)
            {
                _ListData.Clear();
            }

            _ListData = new List<MyQuantifiedData>();

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

                // there could be days with no data (e.g. of phone was turned off)
                if(_history.Count>0)
                {
                    // first entry may be from previous time window, is there any data from current time window?
                    bool hasDataInTimeWindow = false;
                    
                    // insert new fist entry, representing the last activity of the previous time window
                    // this helps capture that activity's duration but only from the start of current time window                    
                    ActivityMonitorReading first = _history[0];
                    if(first.Timestamp <= DateTime.Now.Date.AddDays(timeWindowIndex))
                    {
                        // create new "first" entry, with the same mode but timestamp set as 0:00h in current time window
                        _history.Insert(1, new ActivityMonitorReading(first.Mode, DateTime.Now.Date.AddDays(timeWindowIndex)));
                        // remove previous entry
                        _history.RemoveAt(0);
                        hasDataInTimeWindow = _history.Count > 1;
                    }
                    else
                    {
                        // the first entry belongs to the current time window
                        // there is no known activity before it
                        hasDataInTimeWindow = true;
                    }

                    // if at least one activity is recorded in this time window
                    if(hasDataInTimeWindow)
                    {
                        // insert a last activity, marking the begining of the next time window
                        // this helps capturing the correct duration of the last activity stated in this time window
                        ActivityMonitorReading last = _history.Last();
                        if (last.Timestamp < DateTime.Now.Date.AddDays(timeWindowIndex + 1))
                        {
                            // is this today's time window
                            if (timeWindowIndex == 0)
                            {
                                // last activity duration measured until this instant time
                                _history.Add(new ActivityMonitorReading(last.Mode, DateTime.Now));
                            }
                            else
                            {
                                // last activity measured until the begining of the next time index
                                _history.Add(new ActivityMonitorReading(last.Mode, DateTime.Now.Date.AddDays(timeWindowIndex + 1)));
                            }
                        }

                        // calculate duration for each current activity by subtracting its timestamp from that of the next one
                        for (int i = 0; i < _history.Count - 1; i++)
                        {
                            ActivityMonitorReading current = _history[i];
                            ActivityMonitorReading next = _history[i + 1];

                            _durations[indexer[current.Mode]] += next.Timestamp - current.Timestamp;
                        }
                    }
                }

                // populate the list to be displayed in the UI
                for (int i = 0; i < _activitiesList.Count; i++)
                {
                    _ListData.Add(new MyQuantifiedData(_activitiesList[i], _durations[i]));
                }
            }

            NotifyPropertyChanged("ListData");
        }

    }

    public class MyQuantifiedData
    {
        public MyQuantifiedData(string s, TimeSpan i)
        {
            ActivityName = s;
            ActivityTime = i;
        }

        public string ActivityName
        {
            get;
            set;
        }

        public TimeSpan ActivityTime
        {
            get;
            set;
        }
    }
}
