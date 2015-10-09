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
        private IActivitySensor _sensor = null;

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
                        _sensor = await ActivitySensorFactory.GetDefaultAsync();

                        // Bind data
                        DataContext = _sensor.GetActivityDataInstance();
                    }

                    // Register delegate to get reading changes
                    _sensor.ReadingChanged += activity_ReadingChanged;

                    // Activate the sensor
                    await _sensor.ActivateAsync();

                    // Update screen
                    await UpdateSummaryAsync();
                }
            };
        }

        /// <summary>
        /// Called when navigating to this page
        /// </summary>
        /// <param name="e">Event arguments</param>
        protected async override void OnNavigatedTo( NavigationEventArgs e )
        {
            if( e.NavigationMode == NavigationMode.Back )
            {
                // Register for reading change notifications if we have already not registered.
                _sensor.ReadingChanged += activity_ReadingChanged;

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
                // Call into the actual sensor implementation to update data
                // source.
                ((IActivitySensor)sender).UpdateCurrentActivity(args);
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
                await _sensor.UpdateSummaryAsync(_dayOffset);
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