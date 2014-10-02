using Lumia.Sense;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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

                // skip fist returned entry, it is outside of our requested time interval
                for (int i = 1; i < _history.Count - 1; i++)
                {
                    ActivityMonitorReading current = _history[i];
                    ActivityMonitorReading next = _history[i + 1];

                    _durations[indexer[current.Mode]] += next.Timestamp - current.Timestamp;
                }

                for (int i = 0; i < _activitiesList.Count; i++)
                {
                    _ListData.Add(new MyQuantifiedData(_activitiesList[i], _durations[i]));
                }
            }

            Debug.WriteLine("ListData changed");
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
