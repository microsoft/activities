/*
 * Copyright (c) 2014 Microsoft Mobile. All rights reserved.
 * See the license text file provided with this project for more information.
 */

using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Windows;
using System.Collections.Generic;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Lumia.Sense;
using System.Diagnostics;
using ActivitiesExample.Data;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Lumia.Sense.Testing;
using Windows.ApplicationModel.Resources;

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
        private bool _runningInEmulator = false;
        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView("Resources");
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
            if(x.SystemProductName.StartsWith("Virtual"))
            {
                _runningInEmulator = true;
            }
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {       
            if (!_runningInEmulator && !await ActivityMonitor.IsSupportedAsync())
            {
                // nothing to do if we cannot use the API
                // In a real app please do make an effort to create a better user experience.
                // e.g. if access to Activity Monitor is not mission critical, let the app run without showing any error message
                // simply hide the features that depend on it
                MessageDialog md = new MessageDialog(this.resourceLoader.GetString("FeatureNotSupported/Message"), this.resourceLoader.GetString("FeatureNotSupported/Title"));
                await md.ShowAsync(); 
                Application.Current.Exit();
            }

            Initialize();
        }

        /// <summary>
        /// Initializes activity monitor
        /// </summary>
        private async void Initialize()
        {
            if (_activityMonitor == null)
            {
                if(_runningInEmulator)
                {
                    await CallSensorCoreApiAsync(async () => { _activityMonitor = await ActivityMonitorSimulator.GetDefaultAsync(); });
                }
                else
                {
                    await CallSensorCoreApiAsync(async () => { _activityMonitor = await ActivityMonitor.GetDefaultAsync(); });
                }

                if (_activityMonitor!=null)
                {
                    // Set activity observer
                    _activityMonitor.ReadingChanged += activityMonitor_ReadingChanged;
                    _activityMonitor.Enabled = true;

                    // read current activity
                    ActivityMonitorReading reading = null;
                    if (await CallSensorCoreApiAsync(async () => { reading = await _activityMonitor.GetCurrentReadingAsync(); }))
                    {
                        if (reading != null)
                        {
                            MyData.Instance().ActivityEnum = reading.Mode;
                        }
                    }

                    // read logged data
                    PollHistory();
                }
                else
                {
                    // nothing to do if we cannot use the API
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
        /// <param name="action"></param>
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
                            dialog = new MessageDialog(this.resourceLoader.GetString("FeatureDisabled/Location"), this.resourceLoader.GetString("FeatureDisabled/Title"));
                            dialog.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchLocationSettingsAsync())));
                            dialog.Commands.Add(new UICommand("No"));
                            await dialog.ShowAsync();
                            new System.Threading.ManualResetEvent(false).WaitOne(500);
                            return false;

                        case SenseError.SenseDisabled:
                            dialog = new MessageDialog(this.resourceLoader.GetString("FeatureDisabled/MotionData"), this.resourceLoader.GetString("FeatureDisabled/InTitle"));
                            dialog.Commands.Add(new UICommand("Yes", new UICommandInvokedHandler(async (cmd) => await SenseHelper.LaunchSenseSettingsAsync())));
                            dialog.Commands.Add(new UICommand("No"));
                            await dialog.ShowAsync();
                            new System.Threading.ManualResetEvent(false).WaitOne(500);
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
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (_activityMonitor != null)
            {
                _activityMonitor.ReadingChanged += activityMonitor_ReadingChanged;
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
                    // read all the activities recorded today by the Lumia SensorCore
                    MyData.Instance().History = await _activityMonitor.GetActivityHistoryAsync(DateTime.Now.Date, DateTime.Now - DateTime.Now.Date);
                }))
                {
                    Debug.WriteLine("Reading the history failed.");
                }
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AboutPage));
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            PollHistory();
        }
    }
}

