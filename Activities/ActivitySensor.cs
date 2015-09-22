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
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace ActivitiesExample
{
    public delegate void ReadingChangedEventHandler(object sender, object args);
    /// <summary>
    /// Platform agnostic Activity Sensor interface. 
    /// This interface is implementd by OSActivitySensor and LumiaActivitySensor.
    /// </summary>
    public interface IActivitySensor
    {
        /// <summary>
        /// Initializes the sensor.
        /// </summary>
        /// <returns>Asynchronous task</returns>
        Task InitializeSensorAsync();

        /// <summary>
        /// Activates the sensor and registers for reading changed notifications.
        /// </summary>
        /// <returns>Asynchronous task</returns>
        Task ActivateAsync();

        /// <summary>
        /// Deactivates sensor the sensor and registers for reading changed notifications.
        /// </summary>
        /// <returns>Asynchronous task</returns>
        Task DeactivateAsync();

        /// <summary>
        /// Pull activity entries from history database and populate the internal list.
        /// </summary>
        /// <param name="DayOffset">DayOffset from current day</param>
        /// <returns>Asynchronous task</returns>
        Task UpdateSummaryAsync(uint DayOffset);

        /// <summary>
        /// Update current cached activity of the user.
        /// </summary>
        /// <param name="args">Current Activity reported by the sensor. Type of this argument is either Windows.Devices.Sensors.ActivityType or Lumia.Sense.Activity</param>
        void UpdateCurrentActivity(object args);

        /// <summary>
        /// Get an instance of ActivityData<T>. This is the data source that reflects 
        /// the history entries that gets displayed in the UI.
        /// </summary>
        object GetActivityDataInstance();

        /// <summary>
        /// Delegate for receving reading changed events.
        /// </summary>
        event ReadingChangedEventHandler ReadingChanged;
    }

    /// <summary>
    /// Factory class for instantiating Activity Sensor. If there an activity sensor surfaced
    /// through Windows.Devices.Sensor then the factory creates an instance of OSActivitySensor
    /// otherwise this falls back to using LumiaActivitySensor.
    /// </summary>
    public static class ActivitySensorFactory
    {
        /// <summary>
        /// Static method to get the default activity sensor present in the system.
        /// </summary>
        public static async Task<IActivitySensor> GetDefaultAsync()
        {
            IActivitySensor sensor = null;
            
            try
            {
                // Check if there is an activity sensor in the system
                ActivitySensor activitySensor = await ActivitySensor.GetDefaultAsync();

                // If there is one then create OSActivitySensor.
                if (activitySensor != null)
                {
                    sensor = new OSActivitySensor(activitySensor);
                }
            }
            catch (System.UnauthorizedAccessException)
            {
                // If there is an activity sensor but the user has disabled motion data
                // then check if the user wants to open settngs and enable motion data.
                MessageDialog dialog = new MessageDialog("Motion access has been disabled in system settings. Do you want to open settings now?", "Information");
                dialog.Commands.Add(new UICommand("Yes", async cmd => await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-motion"))));
                dialog.Commands.Add(new UICommand("No"));
                await dialog.ShowAsync();
                new System.Threading.ManualResetEvent(false).WaitOne(500);
                return null;
            }
            
            // If the OS activity sensor is not present then create the LumiaActivitySensor.
            // This will use ActivityMonitor from SensorCore.
            if (sensor == null)
            {
                // Check if all the required settings have been configured correctly
                await LumiaActivitySensor.ValidateSettingsAsync();

                sensor = new LumiaActivitySensor();
            }
            return sensor;
        }
    }

    /// <summary>
    /// Implementation of IActivitySensor that surfaces Activity Sensor supported by the OS (Windows.Devices.Sensor.ActivitySensor).
    /// </summary>
    public class OSActivitySensor : IActivitySensor 
    {
        #region Private members
        /// <summary>
        /// Singleton instance.
        /// </summary>
        protected static OSActivitySensor _self;

        /// <summary>
        /// Physical sensor.
        /// </summary>
        private static Windows.Devices.Sensors.ActivitySensor _sensor = null;

        /// <summary>
        /// Constructs a new ResourceLoader object.
        /// </summary>
        static protected readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        /// <summary>
        /// Check if running in emulator.
        /// </summary>
        protected bool _runningInEmulator = false;

        /// <summary>
        /// Reading changed handler.
        /// </summary
        public event ReadingChangedEventHandler ReadingChanged = null;
        #endregion

        /// <summary>
        /// OSActivitySensor constructor.
        /// </summary
        public OSActivitySensor(ActivitySensor sensor)
        {
            _sensor = sensor;
            // Using this method to detect if the application runs in the emulator or on a real device. Later the *Simulator API is used to read fake sense data on emulator. 
            // In production code you do not need this and in fact you should ensure that you do not include the Lumia.Sense.Test reference in your project.
            EasClientDeviceInformation x = new EasClientDeviceInformation();
            if (x.SystemProductName.StartsWith("Virtual"))
            {
                _runningInEmulator = true;
            }
        }

        /// <summary>
        /// Get the singleton instance of ActivityData<Windows.Devices.Sensors.ActivityType>.
        /// </summary>
        /// <returns>ActivityData/returns>
        public object GetActivityDataInstance()
        {
            return ActivityData<Windows.Devices.Sensors.ActivityType>.Instance();
        }

        /// <summary>
        /// Initialize sensor.
        /// </summary>
        /// <returns>Asynchronous Task</returns>
        public Task InitializeSensorAsync()
        {
            // Subscribe to all supported acitivities
            foreach (ActivityType activity in _sensor.SupportedActivities)
            {
                _sensor.SubscribedActivities.Add(activity);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Activate the sensor. For activity sensor exposed through 
        /// Windows.Devices.Sensor register reading changed handler.
        /// </summary>
        /// <returns>Asynchronous task/returns>
        public Task ActivateAsync()
        {
            _sensor.ReadingChanged += new TypedEventHandler<ActivitySensor, ActivitySensorReadingChangedEventArgs>(ActivitySensor_ReadingChanged);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Deactivate the sensor. For activity sensor exposed through 
        /// Windows.Devices.Sensor unregister reading changed handler.
        /// </summary>
        /// <returns>Asynchronous task/returns>
        public Task DeactivateAsync()
        {
            _sensor.ReadingChanged -= new TypedEventHandler<ActivitySensor, ActivitySensorReadingChangedEventArgs>(ActivitySensor_ReadingChanged);
            return Task.FromResult(false);
        }

        /// <summary>
        /// Update the reading in the screen.
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
        /// Returns the activity at the given time
        /// </summary>
        /// <param name="sensor">Sensor instance</param>
        /// <param name="timestamp">Time stamp</param>
        /// <returns>Activity at the given time or <c>null</c> if no activity is found.</returns>
        public static async Task<ActivitySensorReading> GetActivityAtAsync(DateTimeOffset timestamp)
        {
            // We assume here that one day overshoot is enough to cover most cases. If the previous activity lasted longer
            // than that, we will miss it. Overshoot duration can be extended but will decrease performance.
            TimeSpan overshoot = TimeSpan.FromDays(1);
            IReadOnlyList<ActivitySensorReading> history = await ActivitySensor.GetSystemHistoryAsync(
                timestamp - overshoot,
                overshoot);
            if (history.Count > 0)
            {
                return history[history.Count - 1];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Updates the summary in the screen.
        /// </summary>
        /// <returns>Asynchronous task/returns>
        /// <param name="DayOffset">Day offset</param>
        /// <returns>Asyncrhonous Task</returns>
        public async Task UpdateSummaryAsync(uint DayOffset)
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

            if (history.Count == 0 || history[0].Timestamp > startDate)
            {
                ActivitySensorReading currentReading = await GetActivityAtAsync(startDate);
                if (currentReading != null)
                {
                    List<ActivitySensorReading> finalHistory = new List<ActivitySensorReading>(history);
                    finalHistory.Insert(0, currentReading);
                    history = finalHistory.AsReadOnly();
                }
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
        /// Update the current activity that's displayed.
        /// </summary>
        /// <param name="args">Event arguments</param>
        public void UpdateCurrentActivity(object args)
        {
            ActivityData<Windows.Devices.Sensors.ActivityType>.Instance().CurrentActivity = (Windows.Devices.Sensors.ActivityType)args;
        }
    };

    /// <summary>
    /// Implementation of IActivitySensor that surfaces Activity Sensor supported by the SensorCore.
    /// instance. 
    /// </summary>
    public class LumiaActivitySensor : IActivitySensor
    {
        #region Private members
        /// <summary>
        /// Physical sensor.
        /// </summary>
        public static Lumia.Sense.ActivityMonitor _activityMonitor = null;

        /// <summary>
        /// Constructs a new ResourceLoader object.
        /// </summary>
        static protected readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        /// <summary>
        /// Check if running in emulator
        /// </summary>
        protected bool _runningInEmulator = false;

        /// <summary>
        /// Reading changed handler.
        /// </summary
        public event ReadingChangedEventHandler ReadingChanged = null;
        #endregion

        /// <summary>
        /// Lumia Activity Sensor constructor.
        /// </summary
        public LumiaActivitySensor()
        {

        }

        /// <summary>
        /// Performs asynchronous Sense SDK operation and handles any exceptions
        /// </summary>
        /// <param name="action">The function delegate to execute asynchronously when one task in the tasks completes</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwis:)
        /// e</returns>
        private async Task<bool> CallSensorCoreApiAsync(Func<Task> action)
        {
            Exception failure = null;
            try
            {
                await action();
            }
            catch (Exception e)
            {
                failure = e;
                Debug.WriteLine("Failure:" + e.Message);
            }
            if (failure != null)
            {
                try
                {
                    MessageDialog dialog = null;
                    switch (SenseHelper.GetSenseError(failure.HResult))
                    {
                        case SenseError.LocationDisabled:
                            {
                                dialog = new MessageDialog("In order to recognize activities you need to enable location in system settings. Do you want to open settings now? If not, application will exit.", "Information");
                                dialog.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchLocationSettingsAsync())));
                                dialog.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) => { Application.Current.Exit(); })));
                                await dialog.ShowAsync();
                                new System.Threading.ManualResetEvent(false).WaitOne(500);
                                return false;
                            }
                        case SenseError.SenseDisabled:
                            {
                                dialog = new MessageDialog("In order to recognize activities you need to enable Motion data in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information");
                                dialog.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchSenseSettingsAsync())));
                                dialog.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) => { Application.Current.Exit(); })));
                                await dialog.ShowAsync();
                                return false;
                            }
                        default:
                            dialog = new MessageDialog("Failure: " + SenseHelper.GetSenseError(failure.HResult), "");
                            await dialog.ShowAsync();
                            return false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to handle failure. Message:" + ex.Message);
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Get singleton instance of ActivityData<Lumia.Sense.Activity>.
        /// </summary>
        public object GetActivityDataInstance()
        {
            return ActivityData<Lumia.Sense.Activity>.Instance();
        }

        /// <summary>
        /// Initialize sensor core.
        /// </summary>
        /// <returns>Asynchronous task/returns>
        public async Task InitializeSensorAsync()
        {
            // Make sure all necessary settings are enabled
            await ValidateSettingsAsync();

            if (_runningInEmulator)
            {
                // await CallSensorCoreApiAsync( async () => { _activityMonitor = await ActivityMonitorSimulator.GetDefaultAsync(); } );
            }
            else
            {
                // Get the activity monitor instance
                await CallSensorCoreApiAsync(async () =>
                {
                    _activityMonitor = await ActivityMonitor.GetDefaultAsync();
                });
            }
            if (_activityMonitor == null)
            {
                // Nothing to do if we cannot use the API
                Application.Current.Exit();
            }
        }

        /// <summary>
        /// Validate if settings have been configured correctly to run SensorCore.
        /// </summary>
        /// <returns>Asynchronous task/returns>
        public static async Task ValidateSettingsAsync()
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
        /// Activate Sensor Instance.
        /// </summary>
        /// <returns>Asynchronous task/returns>
        public async Task ActivateAsync()
        {
            if (_activityMonitor == null)
            {
                await InitializeSensorAsync();
            }
            else
            {
                _activityMonitor.Enabled = true;
                _activityMonitor.ReadingChanged += activityMonitor_ReadingChanged;

                await CallSensorCoreApiAsync(async () =>
                {
                    await _activityMonitor.ActivateAsync();
                });
            }

        }

        /// <summary>
        /// Deactivate sensor instance.
        /// </summary>
        /// <returns>Asynchronous task/returns>
        public async Task DeactivateAsync()
        {
            if (_activityMonitor != null)
            {
                _activityMonitor.Enabled = false;
                _activityMonitor.ReadingChanged -= activityMonitor_ReadingChanged;
                await CallSensorCoreApiAsync(async () =>
                {
                    await _activityMonitor.DeactivateAsync();
                });
            }

        }

        /// <summary>
        /// Called when activity changes.
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
        /// Update Summary.
        /// </summary>
        /// <returns>Asynchronous task/returns>
        /// <param name="DayOffset">Day Offset</param>
        public async Task UpdateSummaryAsync(uint DayOffset)
        {
            // Read current activity
            ActivityMonitorReading reading = null;

            await CallSensorCoreApiAsync(async () =>
            {
                reading = await _activityMonitor.GetCurrentReadingAsync();
            });

            if (reading != null)
            {
                ActivityData<Lumia.Sense.Activity>.Instance().CurrentActivity = reading.Mode;
            }

            // Fetch activity history for the day
            DateTime startDate = DateTime.Today.Subtract(TimeSpan.FromDays(DayOffset));
            DateTime endDate = startDate + TimeSpan.FromDays(1);
            IList<ActivityMonitorReading> history = null;

            await CallSensorCoreApiAsync(async () =>
            {
                 history = await _activityMonitor.GetActivityHistoryAsync(startDate, TimeSpan.FromDays(1));
            });

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
        /// Update Current Activity that's cached.
        /// </summary>
        /// <param name="args">Current acitivity value</param>
        public void UpdateCurrentActivity(object args)
        {
            ActivityData<Lumia.Sense.Activity>.Instance().CurrentActivity = (Lumia.Sense.Activity)args;
        }
    };
}
