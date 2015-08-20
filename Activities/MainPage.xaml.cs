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
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Core;
using Windows.UI.Popups;

namespace ActivitiesExample
{
    /// <summary>
    /// Application main page
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Private members
        /// <summary>
        /// Activity Sensor instance
        /// </summary>        
        private ActivitySensorInstance _sensor = null;

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

            Window.Current.VisibilityChanged += async ( sender, args ) =>
            {
                await CallSensorCoreApiAsync( async () =>
                {
                    if( !args.Visible )
                    {
                        // Application put to background, deactivate sensor
                        if(_sensor != null)
                        {
                            await _sensor.DeactivateAsync();
                        }
                    }
                    else
                    {
                        // Create sensor instance if already not created
                        if (_sensor == null)
                        {
                            _sensor = await ActivitySensorInstance.GetInstance();

                            // Bind data
                            DataContext = _sensor.GetActivityDataInstance();
                        }

                        // Check if all the required settings have been configured correctly
                        await _sensor.ValidateSettingsAsync();

                        // Register delegate to get reading changes
                        _sensor.ReadingChanged += activity_ReadingChanged;

                        // Activate the sensor
                        await _sensor.ActivateAsync();

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
            // Initialize sensor core
            await _sensor.InitializeSensorCoreAsync();
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
                // Make sure all necessary settings are enabled
                await _sensor.ValidateSettingsAsync();

                // Register for reading change notifications if we have already not registered.
                if(_sensor.ReadingChanged == null)
                {
                    _sensor.ReadingChanged += activity_ReadingChanged;
                }

                // Activate the sensor
                await _sensor.ActivateAsync();

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
            if(_sensor != null )
            {
                // Unregister from reading change notifications
                _sensor.ReadingChanged -= activity_ReadingChanged;
                // Deactivate sensor
                await _sensor.DeactivateAsync();
            }
        }

        /// <summary>
        /// Called when activity changes
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="args">Event arguments</param>
        private async void activity_ReadingChanged(object sender, object args)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Call into the actual sensor implementationt to update data
                // source.
                ((ActivitySensorInstance)sender).UpdateCurrentActivity(args);
            });
        }

        /// <summary>
        /// Poll history, read the data for the past day
        /// </summary>
        /// <returns>Asynchronous task</returns>
        private async Task UpdateSummaryAsync()
        {
            if ( _sensor != null )
            {
                if( !await CallSensorCoreApiAsync( async () =>
                {
                    // Call into the actual sensor implementationt to fetch 
                    // history and update data source
                    await _sensor.UpdateSummaryAsync(_dayOffset);
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

        /// <summary>
        /// Decrease opacity of the command bar when closed
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void CommandBar_Closed(object sender, object e)
        {
            cmdBar.Opacity = 0.5;
        }

        /// <summary>
        /// Increase opacity of command bar when opened
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void CommandBar_Opened(object sender, object e)
        {
            cmdBar.Opacity = 1;
        }
    }
}