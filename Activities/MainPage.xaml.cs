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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Lumia.Sense;
using System.Diagnostics;
using ActivitiesExample.Data;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Lumia.Sense.Testing;
using Windows.ApplicationModel.Resources;
using ActivitiesExample.ActivateSensorCore;

/// <summary>
/// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556
/// </summary>
namespace ActivitiesExample
{
    /// <summary>
    /// Application main page
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Private members
        /// <summary>
        /// Activity monitor instance
        /// </summary>
        private IActivityMonitor _activityMonitor = null;

        /// <summary>
        /// Check if running in emulator
        /// </summary>
        private bool _runningInEmulator = false;

        /// <summary>
        /// Constructs a new ResourceLoader object
        /// </summary>
        private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView("Resources");

        ///  The application model
        /// </summary>
        private ActivitiesExample.App _app = Application.Current as ActivitiesExample.App;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            DataContext = MyData.Instance();
            Loaded += MainPage_Loaded;
            //Using this method to detect if the application runs in the emulator or on a real device. Later the *Simulator API is used to read fake sense data on emulator. 
            //In production code you do not need this and in fact you should ensure that you do not include the Lumia.Sense.Test reference in your project.
            EasClientDeviceInformation x = new EasClientDeviceInformation();
            if (x.SystemProductName.StartsWith("Virtual"))
            {
                _runningInEmulator = true;
            }
        }

        /// <summary>
        /// Loaded event raised after the component is initialized
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event argument</param>
        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_runningInEmulator && !await ActivityMonitor.IsSupportedAsync())
            {
                // Nothing to do if we cannot use the API
                // In a real app please do make an effort to create a better user experience.
                // e.g. if access to Activity Monitor is not mission critical, let the app run without showing any error message
                // simply hide the features that depend on it
                MessageDialog md = new MessageDialog(this._resourceLoader.GetString("FeatureNotSupported/Message"), this._resourceLoader.GetString("FeatureNotSupported/Title"));
                await md.ShowAsync();
                Application.Current.Exit();
            }
        }

        /// <summary>
        /// Initializes activity monitor
        /// </summary>
        private async void Initialize()
        {
            if (!(await ActivityMonitor.IsSupportedAsync()))
            {
                MessageDialog dlg = new MessageDialog("Unfortunately this device does not support activities.");
                await dlg.ShowAsync();
                Application.Current.Exit();
            }
            else
            {
                uint apiSet = await SenseHelper.GetSupportedApiSetAsync();
                MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                // Devices with old location settings
                if (settings.Version < 2 && !settings.LocationEnabled)
                {
                    MessageDialog dlg = new MessageDialog("In order to recognize activities you need to enable location in system settings. Do you want to open settings now? if no, applicatoin will exit", "Information");
                    dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchLocationSettingsAsync())));
                    dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) =>{ Application.Current.Exit();})));
                    await dlg.ShowAsync();
                }
                if (!settings.PlacesVisited)
                {
                    MessageDialog dlg = null;
                    if (settings.Version < 2)
                    {
                        //device which has old motion data settings.
                        //this is equal to motion data settings on/off in old system settings(SDK1.0 based)
                        dlg = new MessageDialog("In order to recognize activities you need to enable Motion data in Motion data settings. Do you want to open settings now? if no, application will exit", "Information");
                        dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchSenseSettingsAsync())));
                        dlg.Commands.Add(new UICommand("No", new UICommandInvokedHandler((cmd) =>{ Application.Current.Exit();})));
                        await dlg.ShowAsync();
                    }
                    else
                    {
                        dlg = new MessageDialog("In order to recognize activities you need to 'enable Places visited' and 'DataQuality to detailed' in Motion data settings. Do you want to open settings now? ", "Information");
                        dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchSenseSettingsAsync())));
                        dlg.Commands.Add(new UICommand("No"));
                        await dlg.ShowAsync();
                    }
                }
                else if (apiSet >= 3 && settings.DataQuality == DataCollectionQuality.Basic)
                {
                    MessageDialog dlg = new MessageDialog("In order to recognize biking you need to enable detailed data collection in Motion data settings. Do you want to open settings now?", "Information");
                    dlg.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchSenseSettingsAsync())));
                    dlg.Commands.Add(new UICommand("No"));
                    await dlg.ShowAsync();
                }
            }
            if (_activityMonitor == null)
            {
                if (_runningInEmulator)
                {
                    if (await CallSensorCoreApiAsync(async () => { _activityMonitor = await ActivityMonitorSimulator.GetDefaultAsync(); }))
                    {
                        Debug.WriteLine("ActivityMonitorSimulator initialized.");
                    }
                    else return;
                }
                else
                {
                    if (await CallSensorCoreApiAsync(async () => { _activityMonitor = await ActivityMonitor.GetDefaultAsync(); }))
                    {
                        Debug.WriteLine("ActivityMonitor initialized.");
                    }
                    else return;
                }
                if (_activityMonitor != null)
                {
                    // Set activity observer
                    _activityMonitor.ReadingChanged += activityMonitor_ReadingChanged;
                    _activityMonitor.Enabled = true;
                    // Read current activity
                    ActivityMonitorReading reading = null;
                    if (await CallSensorCoreApiAsync(async () => { reading = await _activityMonitor.GetCurrentReadingAsync(); }))
                    {
                        if (reading != null)
                        {
                            MyData.Instance().ActivityEnum = reading.Mode;
                        }
                    }
                    // Read logged data
                    PollHistory();
                }
                else
                {
                    // Nothing to do if we cannot use the API
                    // in a real app do make an effort to make the user experience better
                    Application.Current.Exit();
                }
                // Must call DeactivateAsync() when the application goes to background
                Window.Current.VisibilityChanged += async (sender, args) =>
                {
                    if (_activityMonitor != null)
                    {
                        await CallSensorCoreApiAsync(async () =>
                        {
                            if (!args.Visible)
                            {
                                await _activityMonitor.DeactivateAsync();
                            }
                            else
                            {
                                await _activityMonitor.ActivateAsync();
                            }
                        });
                    }
                };
            }
        }

        /// <summary>
        /// Performs asynchronous Sense SDK operation and handles any exceptions
        /// </summary>
        /// <param name="action">The function delegate to execute asynchronously when one task in the tasks completes</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
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
                        case SenseError.SenseDisabled:
                            if (!_app.SensorCoreActivationStatus.onGoing)
                            {
                                this.Frame.Navigate(typeof(ActivateSensorCore.ActivateSensorCore));
                            }
                            return false;
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
        /// Called when navigating to this page
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            ActivateSensorCoreStatus status = _app.SensorCoreActivationStatus;
            if (e.NavigationMode == NavigationMode.Back && status.onGoing)
            {
                status.onGoing = false;
                if (status.activationRequestResult != ActivationRequestResults.AllEnabled)
                {
                    MessageDialog dialog = new MessageDialog(_resourceLoader.GetString("NoLocationOrMotionDataError/Text"), _resourceLoader.GetString("Information/Text"));
                    dialog.Commands.Add(new UICommand("Ok", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchSenseSettingsAsync())));
                    await dialog.ShowAsync();
                    new System.Threading.ManualResetEvent(false).WaitOne(500);
                    Application.Current.Exit();
                }
            }
            if (_activityMonitor != null)
            {
                _activityMonitor.ReadingChanged += activityMonitor_ReadingChanged;
            }
            else
            {
                Initialize();
            }
        }

        /// <summary>
        /// Called when navigating from this page
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_activityMonitor != null)
            {
                _activityMonitor.ReadingChanged -= activityMonitor_ReadingChanged;
            }
        }

        /// <summary>
        /// Called when activity changes
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="args">Event arguments</param>
        private async void activityMonitor_ReadingChanged(IActivityMonitor sender, ActivityMonitorReading args)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MyData.Instance().ActivityEnum = args.Mode;
            });
        }

        /// <summary>
        /// Poll history, read the data for the past day
        /// </summary>
        private async void PollHistory()
        {
            if (_activityMonitor != null)
            {
                if (!await CallSensorCoreApiAsync(async () =>
                {
                    // Get the data for the current 24h time window
                    MyData.Instance().History = await _activityMonitor.GetActivityHistoryAsync(DateTime.Now.Date.AddDays(MyData.Instance().TimeWindow), new TimeSpan(24, 0, 0));
                }))
                {
                    Debug.WriteLine("Reading the history failed.");
                }
            }
        }

        /// <summary>
        /// Navigate to about page
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        /// <summary>
        /// Refresh data for the current day
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            PollHistory();
        }

        /// <summary>
        /// Read the data for the past day
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            // Move the time window 24 to the past
            MyData.Instance().PreviousDay();
            nextButton.IsEnabled = true;
            prevButton.IsEnabled = MyData.Instance().TimeWindow > -10;
            refreshButton.IsEnabled = false;
            PollHistory();
        }

        /// <summary>
        /// Read the data for the next day
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // Move the time window 24h to the present
            MyData.Instance().NextDay();
            nextButton.IsEnabled = MyData.Instance().TimeWindow < 0;
            prevButton.IsEnabled = true;
            refreshButton.IsEnabled = MyData.Instance().TimeWindow == 0;
            PollHistory();
        }
    }
}