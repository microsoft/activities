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
using Windows.ApplicationModel.Resources;
using System.Collections.Generic;

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
        private readonly ResourceLoader _resourceLoader = ResourceLoader.GetForCurrentView( "Resources" );

        /// <summary>
        /// Day offset from day, i.e. 0 = today, 1 = yesterday etc.
        /// </summary>
        private uint _dayOffset = 0;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            DataContext = ActivityData.Instance();

            // Using this method to detect if the application runs in the emulator or on a real device. Later the *Simulator API is used to read fake sense data on emulator. 
            // In production code you do not need this and in fact you should ensure that you do not include the Lumia.Sense.Test reference in your project.
            EasClientDeviceInformation x = new EasClientDeviceInformation();
            if( x.SystemProductName.StartsWith( "Virtual" ) )
            {
                _runningInEmulator = true;
            }

            Window.Current.VisibilityChanged += async ( sender, args ) =>
            {
                await CallSensorCoreApiAsync( async () =>
                {
                    if( !args.Visible )
                    {
                        // Application put to background, deactivate sensor and unregister change observer
                        if( _activityMonitor != null )
                        {
                            _activityMonitor.Enabled = true;
                            _activityMonitor.ReadingChanged -= activityMonitor_ReadingChanged;
                            await _activityMonitor.DeactivateAsync();
                        }
                    }
                    else
                    {
                        // Make sure all necessary settings are enabled in order to run SensorCore
                        await ValidateSettingsAsync();
                        // Make sure sensor is activated
                        if( _activityMonitor == null )
                        {
                            await InitializeSensorAsync();
                        }
                        else
                        {
                            await _activityMonitor.ActivateAsync();
                        }

                        // Enable change observer
                        _activityMonitor.ReadingChanged += activityMonitor_ReadingChanged;
                        _activityMonitor.Enabled = true;

                        // Update screen
                        await UpdateSummaryAsync();
                    }
                } );
            };
        }

        /// <summary>
        /// Initializes activity monitor sensor
        /// </summary>
        /// <returns>Asynchronous task</returns>
        private async Task InitializeSensorAsync()
        {
            if( _runningInEmulator )
            {
                //                await CallSensorCoreApiAsync( async () => { _activityMonitor = await ActivityMonitorSimulator.GetDefaultAsync(); } );
            }
            else
            {
                await CallSensorCoreApiAsync( async () => { _activityMonitor = await ActivityMonitor.GetDefaultAsync(); } );
            }
            if( _activityMonitor == null )
            {
                // Nothing to do if we cannot use the API
                Application.Current.Exit();
            }
        }

        /// <summary>
        /// Makes sure necessary settings are enabled in order to use SensorCore
        /// </summary>
        /// <returns>Asynchronous task</returns>
        private async Task ValidateSettingsAsync()
        {
            if( !( await ActivityMonitor.IsSupportedAsync() ) )
            {
                MessageDialog dlg = new MessageDialog( this._resourceLoader.GetString( "FeatureNotSupported/Message" ), this._resourceLoader.GetString( "FeatureNotSupported/Title" ) );
                await dlg.ShowAsync();
                Application.Current.Exit();
            }
            else
            {
                uint apiSet = await SenseHelper.GetSupportedApiSetAsync();
                MotionDataSettings settings = await SenseHelper.GetSettingsAsync();
                if( settings.Version < 2 )
                {
                    // Device which has old Motion data settings which requires system location and Motion data be enabled in order to access
                    // ActivityMonitor.
                    if( !settings.LocationEnabled )
                    {
                        MessageDialog dlg = new MessageDialog( "In order to recognize activities you need to enable location in system settings. Do you want to open settings now? If not, application will exit.", "Information" );
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchLocationSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                        await dlg.ShowAsync();
                    }
                    else if( !settings.PlacesVisited )
                    {
                        MessageDialog dlg = new MessageDialog( "In order to recognize activities you need to enable Motion data in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information" );
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchSenseSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                        await dlg.ShowAsync();
                    }
                }
                else if( apiSet >= 3 )
                {
                    if( !settings.LocationEnabled )
                    {
                        MessageDialog dlg = new MessageDialog( "In order to recognize biking you need to enable location in system settings. Do you want to open settings now?", "Helpful tip" );
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchLocationSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No" ) );
                        await dlg.ShowAsync();
                    }
                    else if( settings.DataQuality == DataCollectionQuality.Basic )
                    {
                        MessageDialog dlg = new MessageDialog( "In order to recognize biking you need to enable detailed data collection in Motion data settings. Do you want to open settings now?", "Helpful tip" );
                        dlg.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchSenseSettingsAsync() ) ) );
                        dlg.Commands.Add( new UICommand( "No" ) );
                        await dlg.ShowAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Performs asynchronous Sense SDK operation and handles any exceptions
        /// </summary>
        /// <param name="action">The function delegate to execute asynchronously when one task in the tasks completes</param>
        /// <returns><c>true</c> if call was successful, <c>false</c> otherwise</returns>
        private async Task<bool> CallSensorCoreApiAsync( Func<Task> action )
        {
            Exception failure = null;
            try
            {
                await action();
            }
            catch( Exception e )
            {
                failure = e;
                Debug.WriteLine( "Failure:" + e.Message );
            }
            if( failure != null )
            {
                try
                {
                    MessageDialog dialog = null;
                    switch( SenseHelper.GetSenseError( failure.HResult ) )
                    {
                        case SenseError.LocationDisabled:
                            {
                                dialog = new MessageDialog( "In order to recognize activities you need to enable location in system settings. Do you want to open settings now? If not, application will exit.", "Information" );
                                dialog.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchLocationSettingsAsync() ) ) );
                                dialog.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                                await dialog.ShowAsync();
                                new System.Threading.ManualResetEvent( false ).WaitOne( 500 );
                                return false;
                            }
                        case SenseError.SenseDisabled:
                            {
                                dialog = new MessageDialog( "In order to recognize activities you need to enable Motion data in Motion data settings. Do you want to open settings now? If not, application will exit.", "Information" );
                                dialog.Commands.Add( new UICommand( "Yes", new UICommandInvokedHandler( async ( cmd ) => await SenseHelper.LaunchSenseSettingsAsync() ) ) );
                                dialog.Commands.Add( new UICommand( "No", new UICommandInvokedHandler( ( cmd ) => { Application.Current.Exit(); } ) ) );
                                await dialog.ShowAsync();
                                return false;
                            }
                        default:
                            dialog = new MessageDialog( "Failure: " + SenseHelper.GetSenseError( failure.HResult ), "" );
                            await dialog.ShowAsync();
                            return false;
                    }
                }
                catch( Exception ex )
                {
                    Debug.WriteLine( "Failed to handle failure. Message:" + ex.Message );
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
        protected async override void OnNavigatedTo( NavigationEventArgs e )
        {
            if( e.NavigationMode == NavigationMode.Back )
            {
                // Make sure all necessary settings are enabled in order to run SensorCore
                await ValidateSettingsAsync();
                // Make sure sensor is activated
                if( _activityMonitor == null )
                {
                    await InitializeSensorAsync();
                }
                else
                {
                    await _activityMonitor.ActivateAsync();
                }

                // Register change observer
                _activityMonitor.ReadingChanged += activityMonitor_ReadingChanged;
                _activityMonitor.Enabled = true;
                // Update screen
                await UpdateSummaryAsync();
            }
        }

        /// <summary>
        /// Called when navigating from this page
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected async override void OnNavigatedFrom( NavigationEventArgs e )
        {
            if( _activityMonitor != null )
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
        private async void activityMonitor_ReadingChanged( IActivityMonitor sender, ActivityMonitorReading args )
        {
            await this.Dispatcher.RunAsync( CoreDispatcherPriority.Normal, () =>
            {
                ActivityData.Instance().CurrentActivity = args.Mode;
            } );
        }

        /// <summary>
        /// Poll history, read the data for the past day
        /// </summary>
        /// <returns>Asynchronous task</returns>
        private async Task UpdateSummaryAsync()
        {
            if( _activityMonitor != null )
            {
                if( !await CallSensorCoreApiAsync( async () =>
                {
                    // Read current activity
                    ActivityMonitorReading reading = await _activityMonitor.GetCurrentReadingAsync();
                    if( reading != null )
                    {
                        ActivityData.Instance().CurrentActivity = reading.Mode;
                    }

                    // Fetch activity history for the day
                    DateTime startDate = DateTime.Today.Subtract( TimeSpan.FromDays( _dayOffset ) );
                    DateTime endDate = startDate + TimeSpan.FromDays( 1 );
                    var history = await _activityMonitor.GetActivityHistoryAsync( startDate, TimeSpan.FromDays( 1 ) );
                    Dictionary<Activity, TimeSpan> activitySummary = new Dictionary<Activity, TimeSpan>();
                    var activityTypes = Enum.GetValues( typeof( Activity ) );
                    foreach( var type in activityTypes )
                    {
                        activitySummary[ (Activity)type ] = TimeSpan.Zero;
                    }
                    if( history.Count > 0 )
                    {
                        Activity currentActivity = history[ 0 ].Mode;
                        DateTime currentDate = history[ 0 ].Timestamp.DateTime;
                        foreach( var item in history )
                        {
                            if( item.Timestamp >= startDate )
                            {
                                TimeSpan duration = TimeSpan.Zero;
                                if( currentDate < startDate )
                                {
                                    // If first activity of the day started already yesterday, set start time to midnight.
                                    currentDate = startDate;
                                }
                                if( item.Timestamp > endDate )
                                {
                                    // If last activity extends over to next day, set end time to midnight.
                                    duration = endDate - currentDate;
                                    break;
                                }
                                else
                                {
                                    duration = item.Timestamp - currentDate;
                                }
                                activitySummary[ currentActivity ] += duration;
                            }
                            currentActivity = item.Mode;
                            currentDate = item.Timestamp.DateTime;
                        }
                    }
                    List<ActivityDuration> historyList = new List<ActivityDuration>();
                    foreach( var activityType in activityTypes )
                    {
                        historyList.Add( new ActivityDuration( (Activity)activityType, activitySummary[ (Activity)activityType ] ) );
                    }
                    ActivityData.Instance().History = historyList;
                    ActivityData.Instance().Date = startDate;
                } ) )
                {
                    Debug.WriteLine( "Reading the history failed." );
                }
            }
        }

        /// <summary>
        /// Navigate to about page
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void AboutButton_Click( object sender, RoutedEventArgs e )
        {
            this.Frame.Navigate( typeof( AboutPage ) );
        }

        /// <summary>
        /// Refresh data for the current day
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private async void RefreshButton_Click( object sender, RoutedEventArgs e )
        {
            await UpdateSummaryAsync();
        }

        /// <summary>
        /// Read the data for the past day
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private async void PrevButton_Click( object sender, RoutedEventArgs e )
        {
            if( _dayOffset < 9 )
            {
                _dayOffset++;
                nextButton.IsEnabled = true;
                prevButton.IsEnabled = _dayOffset < 9;
                refreshButton.IsEnabled = false;
                await UpdateSummaryAsync();
            }
        }

        /// <summary>
        /// Read the data for the next day
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private async void NextButton_Click( object sender, RoutedEventArgs e )
        {
            if( _dayOffset > 0 )
            {
                _dayOffset--;
                nextButton.IsEnabled = _dayOffset > 0;
                prevButton.IsEnabled = true;
                refreshButton.IsEnabled = _dayOffset == 0;
                await UpdateSummaryAsync();
            }
        }
    }
}