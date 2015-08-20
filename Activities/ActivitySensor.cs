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
using ActivitiesExample.Data;
using Lumia.Sense;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace ActivitiesExample
{

    /// <summary>
    /// Base class that abstracts the mechanics of talking to a sensor instance. 
    /// Virtual Methods in the base class are wired to talk to Windows.Devices.Sensor.ActivitySensor
    /// </summary>
    class ActivitySensorInstance
    {

        #region Private members
        /// <summary>
        /// Singleton instance
        /// </summary>
        protected static ActivitySensorInstance _self;

        /// <summary>
        /// Physical sensor
        /// </summary>
        private static Windows.Devices.Sensors.ActivitySensor _sensor = null;

        /// <summary>
        /// Constructs a new ResourceLoader object
        /// </summary>
        static protected readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        /// <summary>
        /// Check if running in emulator
        /// </summary>
        protected bool _runningInEmulator = false;

        /// <summary>
        /// Reading changed delegate signature
        /// </summary
        /// <param name="sender">The sender of the event</param>
        /// <param name="args">Event arguments</param>
        public delegate void ReadingChangedEventHandler(object sender, object args);

        /// <summary>
        /// Reading changed handler
        /// </summary
        public ReadingChangedEventHandler ReadingChanged = null;
        #endregion

        public ActivitySensorInstance()
        {
            // Using this method to detect if the application runs in the emulator or on a real device. Later the *Simulator API is used to read fake sense data on emulator. 
            // In production code you do not need this and in fact you should ensure that you do not include the Lumia.Sense.Test reference in your project.
            EasClientDeviceInformation x = new EasClientDeviceInformation();
            if (x.SystemProductName.StartsWith("Virtual"))
            {
                _runningInEmulator = true;
            }
        }

        /// <summary>
        /// Create new instance 
        /// </summary>
        /// <returns>Data instance</returns>
        static public async Task<ActivitySensorInstance> GetInstance()
        {
            // Create the instance if it is not already created
            if (_self == null)
            {
                try
                {
                    // Try to get default Activity Sensor exposed by the operating system
                    // before trying to use activity sensor from SensorCore SDK
                    _sensor = await Windows.Devices.Sensors.ActivitySensor.GetDefaultAsync();

                    // If it's not available fall back to sensor core
                    if (_sensor == null)
                    {
                        _self = new LumiaActivitySensor();
                    }
                    else
                    {
                        _self = new ActivitySensorInstance();
                    }
                }
                catch (System.UnauthorizedAccessException)
                {
                    // If motion data is disabled ask the user to enable it
                    // before falling back to sensor core SDK.
                    MessageDialog dlg = new MessageDialog("You need to authorize this app to use motion data in system settings.");
                    await dlg.ShowAsync();
                    Application.Current.Exit();
                }
            }

            return _self;
        }

        /// <summary>
        /// Get the singleton instance.
        /// </summary>
        /// <returns>ActivityData/returns>
        virtual public object GetActivityDataInstance()
        {
            return ActivityData<Windows.Devices.Sensors.ActivityType>.Instance();
        }

        /// <summary>
        /// Initialize sensor
        /// </summary>
        /// <returns>Asynchronous Task</returns>
        virtual public Task InitializeSensorCoreAsync()
        {
            // Subscribe to all supported acitivities
            foreach (ActivityType activity in _sensor.SupportedActivities)
            {
                _sensor.SubscribedActivities.Add(activity);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Do nothing
        /// </summary>
        /// <returns>Asynchronous task/returns>
        virtual public Task ValidateSettingsAsync()
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Activate the sensor. For activity sensor exposed through 
        /// Windows.Devices.Sensor register reading changed handler.
        /// </summary>
        /// <returns>Asynchronous task/returns>
        virtual public Task ActivateAsync()
        {
            _sensor.ReadingChanged += new TypedEventHandler<ActivitySensor, ActivitySensorReadingChangedEventArgs>(ActivitySensor_ReadingChanged);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Deactivate the sensor. For activity sensor exposed through 
        /// Windows.Devices.Sensor unregister reading changed handler.
        /// </summary>
        /// <returns>Asynchronous task/returns>
        virtual public Task DeactivateAsync()
        {
            _sensor.ReadingChanged -= new TypedEventHandler<ActivitySensor, ActivitySensorReadingChangedEventArgs>(ActivitySensor_ReadingChanged);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Update the reading in the screen
        /// </summary>
        /// <returns>Nothing/returns>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        async private void ActivitySensor_ReadingChanged(object sender, ActivitySensorReadingChangedEventArgs e)
        {
            if (ReadingChanged != null)
            {
                await Task.Run(() =>
                {
                    ActivitySensorReading reading = e.Reading;
                    // Call into the reading changed handler registered by the client
                    ReadingChanged(this, reading.Activity);
                });
            }
        }

        /// <summary>
        /// Updates the summary in the screen
        /// </summary>
        /// <returns>Asynchronous task/returns>
        /// <param name="DayOffset">Day offset</param>
        /// <returns>Asyncrhonous Task</returns>
        virtual public async Task UpdateSummaryAsync(uint DayOffset)
        {
            // Read current activity
            ActivitySensorReading reading = await _sensor.GetCurrentReadingAsync();
            if (reading != null)
            {
                ActivityData<Windows.Devices.Sensors.ActivityType>.Instance().CurrentActivity = reading.Activity;
            }

            // Fetch activity history for the day
            DateTime startDate = DateTime.Today.Subtract(TimeSpan.FromDays(DayOffset));
            DateTime endDate = startDate + TimeSpan.FromDays(1);
            var history = await ActivitySensor.GetSystemHistoryAsync(startDate, TimeSpan.FromDays(1));

            // Create a dictionary to store data
            Dictionary<Windows.Devices.Sensors.ActivityType, TimeSpan> activitySummary = new Dictionary<Windows.Devices.Sensors.ActivityType, TimeSpan>();

            // Initialize timespan for all entries
            var activityTypes = Enum.GetValues(typeof(Windows.Devices.Sensors.ActivityType));
            foreach (var type in activityTypes)
            {
                activitySummary[(Windows.Devices.Sensors.ActivityType)type] = TimeSpan.Zero;
            }

            // Update the timespan for all activities in the dictionary
            if (history.Count > 0)
            {
                Windows.Devices.Sensors.ActivityType currentActivity = history[0].Activity;
                DateTime currentDate = history[0].Timestamp.DateTime;
                foreach (var item in history)
                {
                    if (item.Timestamp >= startDate)
                    {
                        TimeSpan duration = TimeSpan.Zero;
                        if (currentDate < startDate)
                        {
                            // If first activity of the day started already yesterday, set start time to midnight.
                            currentDate = startDate;
                        }
                        if (item.Timestamp > endDate)
                        {
                            // If last activity extends over to next day, set end time to midnight.
                            duration = endDate - currentDate;
                            break;
                        }
                        else
                        {
                            duration = item.Timestamp - currentDate;
                        }
                        activitySummary[currentActivity] += duration;
                    }
                    currentActivity = item.Activity;
                    currentDate = item.Timestamp.DateTime;
                }
            }

            // Prepare the summary to add it to data source
            List<ActivityDuration<Windows.Devices.Sensors.ActivityType>> historyList = new List<ActivityDuration<Windows.Devices.Sensors.ActivityType>>();
            foreach (var activityType in activityTypes)
            {
                // For each entry in the summary add the type and duration to data source
                historyList.Add(new ActivityDuration<Windows.Devices.Sensors.ActivityType>((Windows.Devices.Sensors.ActivityType)activityType, activitySummary[(Windows.Devices.Sensors.ActivityType)activityType]));
            }

            // Update the singleton instance of the data source
            ActivityData<Windows.Devices.Sensors.ActivityType>.Instance().History = historyList;
            ActivityData<Windows.Devices.Sensors.ActivityType>.Instance().Date = startDate;
        }

        /// <summary>
        /// Update the current activity that's displayed
        /// </summary>
        /// <param name="args">Event arguments</param>
        virtual public void UpdateCurrentActivity(object args)
        {
            ActivityData<Windows.Devices.Sensors.ActivityType>.Instance().CurrentActivity = (Windows.Devices.Sensors.ActivityType)args;
        }
    };

    /// <summary>
    /// Helper class that is used to talk to Lumia SensorCore ActivityMonitor 
    /// instance. 
    /// </summary>
    class LumiaActivitySensor : ActivitySensorInstance
    {
        #region Private members
        /// <summary>
        /// Physical sensor
        /// </summary>
        public static Lumia.Sense.ActivityMonitor _activityMonitor = null;
        #endregion

        public LumiaActivitySensor()
        {

        }

        public override object GetActivityDataInstance()
        {
            return ActivityData<Lumia.Sense.Activity>.Instance();
        }

        /// <summary>
        /// Initialize sensor core
        /// </summary>
        /// <returns>Asynchronous task/returns>
        public override async Task InitializeSensorCoreAsync()
        {
            if (_runningInEmulator)
            {
                // await CallSensorCoreApiAsync( async () => { _activityMonitor = await ActivityMonitorSimulator.GetDefaultAsync(); } );
            }
            else
            {
                // Get the activity monitor instance
                _activityMonitor = await ActivityMonitor.GetDefaultAsync();
            }
            if (_activityMonitor == null)
            {
                // Nothing to do if we cannot use the API
                Application.Current.Exit();
            }
        }

        /// <summary>
        /// Validate if settings have been configured correctly to run SensorCore
        /// </summary>
        /// <returns>Asynchronous task/returns>
        public override async Task ValidateSettingsAsync()
        {
            if (!(await ActivityMonitor.IsSupportedAsync()))
            {
                MessageDialog dlg = new MessageDialog(_resourceLoader.GetString("FeatureNotSupported/Message"), _resourceLoader.GetString("FeatureNotSupported/Title"));
                await dlg.ShowAsync();
                Application.Current.Exit();
            }
            else
            {
                uint apiSet = await SenseHelper.GetSupportedApiSetAsync();
                MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                if (settings.Version < 2)
                {
                    // Device which has old Motion data settings which requires system location and Motion data be enabled in order to access
                    // ActivityMonitor.
                    if (!settings.LocationEnabled)
                    {
                        MessageDialog dlg = new MessageDialog("In order to recognize activities you need to enable location in system settings. Do you want to open settings now? If not, application will exit.", "Information");
                        dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchLocationSettingsAsync())));
                        dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) => { Application.Current.Exit(); })));
                        await dlg.ShowAsync();
                    }
                    else if (!settings.PlacesVisited)
                    {
                        MessageDialog dlg = new MessageDialog("In order to recognize activities you need to enable Motion data in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information");
                        dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchSenseSettingsAsync())));
                        dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) => { Application.Current.Exit(); })));
                        await dlg.ShowAsync();
                    }
                }
                else if (apiSet >= 3)
                {
                    if (!settings.LocationEnabled)
                    {
                        MessageDialog dlg = new MessageDialog("In order to recognize biking you need to enable location in system settings. Do you want to open settings now?", "Helpful tip");
                        dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchLocationSettingsAsync())));
                        dlg.Commands.Add(new UICommand("No"));
                        await dlg.ShowAsync();
                    }
                    else if (settings.DataQuality == DataCollectionQuality.Basic)
                    {
                        MessageDialog dlg = new MessageDialog("In order to recognize biking you need to enable detailed data collection in Motion data settings. Do you want to open settings now?", "Helpful tip");
                        dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchSenseSettingsAsync())));
                        dlg.Commands.Add(new UICommand("No"));
                        await dlg.ShowAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Activate Sensor Instance
        /// </summary>
        /// <returns>Asynchronous task/returns>
        public override async Task ActivateAsync()
        {
            if (_activityMonitor == null)
            {
                await InitializeSensorCoreAsync();
            }
            else
            {
                _activityMonitor.Enabled = true;
                _activityMonitor.ReadingChanged += activityMonitor_ReadingChanged;

                await _activityMonitor.ActivateAsync();
            }

        }

        /// <summary>
        /// Deactivate sensor instance
        /// </summary>
        /// <returns>Asynchronous task/returns>
        public override async Task DeactivateAsync()
        {
            if (_activityMonitor != null)
            {
                _activityMonitor.Enabled = false;
                _activityMonitor.ReadingChanged -= activityMonitor_ReadingChanged;
                await _activityMonitor.DeactivateAsync();
            }

        }

        /// <summary>
        /// Called when activity changes
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="args">Event arguments</param>
        private async void activityMonitor_ReadingChanged(IActivityMonitor sender, ActivityMonitorReading args)
        {
            if (ReadingChanged != null)
            {
                await Task.Run(() =>
                {
                    ReadingChanged(this, args.Mode);
                });
            }
        }

        /// <summary>
        /// Update Summary
        /// </summary>
        /// <returns>Asynchronous task/returns>
        /// <param name="DayOffset">Day Offset</param>
        public override async Task UpdateSummaryAsync(uint DayOffset)
        {
            // Read current activity
            ActivityMonitorReading reading = await _activityMonitor.GetCurrentReadingAsync();
            if (reading != null)
            {
                ActivityData<Lumia.Sense.Activity>.Instance().CurrentActivity = reading.Mode;
            }

            // Fetch activity history for the day
            DateTime startDate = DateTime.Today.Subtract(TimeSpan.FromDays(DayOffset));
            DateTime endDate = startDate + TimeSpan.FromDays(1);
            var history = await _activityMonitor.GetActivityHistoryAsync(startDate, TimeSpan.FromDays(1));

            // Create a dictionary to store data
            Dictionary<Activity, TimeSpan> activitySummary = new Dictionary<Activity, TimeSpan>();

            // Initialize timespan for all entries
            var activityTypes = Enum.GetValues(typeof(Activity));
            foreach (var type in activityTypes)
            {
                activitySummary[(Activity)type] = TimeSpan.Zero;
            }

            // Update the timespan for all activities in the dictionary
            if (history.Count > 0)
            {
                Activity currentActivity = history[0].Mode;
                DateTime currentDate = history[0].Timestamp.DateTime;
                foreach (var item in history)
                {
                    if (item.Timestamp >= startDate)
                    {
                        TimeSpan duration = TimeSpan.Zero;
                        if (currentDate < startDate)
                        {
                            // If first activity of the day started already yesterday, set start time to midnight.
                            currentDate = startDate;
                        }
                        if (item.Timestamp > endDate)
                        {
                            // If last activity extends over to next day, set end time to midnight.
                            duration = endDate - currentDate;
                            break;
                        }
                        else
                        {
                            duration = item.Timestamp - currentDate;
                        }
                        activitySummary[currentActivity] += duration;
                    }
                    currentActivity = item.Mode;
                    currentDate = item.Timestamp.DateTime;
                }
            }

            // Prepare the summary to add it to data source
            List<ActivityDuration<Lumia.Sense.Activity>> historyList = new List<ActivityDuration<Lumia.Sense.Activity>>();
            foreach (var activityType in activityTypes)
            {
                // For each entry in the summary add the type and duration to data source
                historyList.Add(new ActivityDuration<Lumia.Sense.Activity>((Activity)activityType, activitySummary[(Activity)activityType]));
            }

            // Update the singleton instance of the data source
            ActivityData<Lumia.Sense.Activity>.Instance().History = historyList;
            ActivityData<Lumia.Sense.Activity>.Instance().Date = startDate;
        }

        /// <summary>
        /// Update Summary
        /// </summary>
        /// <param name="args">Current acitivity value</param>
        public override void UpdateCurrentActivity(object args)
        {
            ActivityData<Lumia.Sense.Activity>.Instance().CurrentActivity = (Lumia.Sense.Activity)args;
        }
    };
}
