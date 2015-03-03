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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

/// <summary>
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556
/// </summary>
namespace ActivitiesExample.ActivateSensorCore
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ActivateSensorCore : Page
    {
        #region Private members
        /// <summary>
        /// SensorCore status
        /// </summary>
        private ActivateSensorCoreStatus _sensorCoreActivationStatus;

        /// <summary>
        /// Display/Hide Dialog for MotionData
        /// </summary>
        private bool _updatingDialog = false;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivateSensorCore()
        {
            this.InitializeComponent();
            var app = Application.Current as ActivitiesExample.App;
            _sensorCoreActivationStatus = app.SensorCoreActivationStatus;
        }

        /// <summary>
        /// Check if motion data is enabled
        /// </summary>
        async Task UpdateDialog()
        {
            if (_updatingDialog || (_sensorCoreActivationStatus.activationRequestResult != ActivationRequestResults.NotAvailableYet))
            {
                return;
            }
            _updatingDialog = true;
            MotionDataActivationBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            LocationActivationBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            Exception failure = null;
            try
            {
                // GetDefaultAsync will throw if MotionData is disabled  
                ActivityMonitor monitor = await ActivityMonitor.GetDefaultAsync();
                // But confirm that MotionData is really enabled by calling ActivateAsync,
                // to cover the case where the MotionData has been disabled after the app has been launched.
                await monitor.ActivateAsync();
            }
            catch (Exception exception)
            {
                switch (SenseHelper.GetSenseError(exception.HResult))
                {
                    case SenseError.LocationDisabled:
                        LocationActivationBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        break;
                    case SenseError.SenseDisabled:
                        MotionDataActivationBox.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        break;
                    default:
                        // Do something clever here
                        break;
                }
                failure = exception;
            }
            if (failure == null)
            {
                // All is good now, dismiss the dialog.
                _sensorCoreActivationStatus.activationRequestResult = ActivationRequestResults.AllEnabled;
                this.Frame.GoBack();
            }
            _updatingDialog = false;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New)
            {
                Window.Current.VisibilityChanged += Current_VisibilityChanged;
                _sensorCoreActivationStatus.onGoing = true;
                _sensorCoreActivationStatus.activationRequestResult = ActivationRequestResults.NotAvailableYet;
            }
            await UpdateDialog();
        }

        /// <summary>
        /// Called when navigating from this page
        /// </summary>
        /// <param name="e">Event argument.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                Window.Current.VisibilityChanged -= Current_VisibilityChanged;
            }
        }

        /// <summary>
        /// Called when the page is visible or not
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments. Contains the arguments returned by the event fired when a CoreWindow instance's visibility changes.</param>
        async void Current_VisibilityChanged(object sender, Windows.UI.Core.VisibilityChangedEventArgs e)
        {
            if (e.Visible)
            {
                await UpdateDialog();
            }
        }

        /// <summary>
        /// Called when later button is clicked
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void LaterButton_Click(object sender, RoutedEventArgs e)
        {
            _sensorCoreActivationStatus.activationRequestResult = ActivationRequestResults.AskMeLater;
            Application.Current.Exit();
        }

        /// <summary>
        /// Called when never button is clicked
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void NeverButton_Click(object sender, RoutedEventArgs e)
        {
            _sensorCoreActivationStatus.activationRequestResult = ActivationRequestResults.NoAndDontAskAgain;
            this.Frame.GoBack();
        }

        /// <summary>
        /// Called when motion data is activated
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private async void MotionDataActivationButton_Click(object sender, RoutedEventArgs e)
        {
            await SenseHelper.LaunchSenseSettingsAsync();
            // Although asynchronous, this completes before the user has actually done anything.
            // The application will loose control, the system settings will be displayed.
            // We will get the control back to our application via a visibilityChanged event.
        }

        /// <summary>
        /// Called when location is activated
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Event arguments</param>
        private async void LocationActivationButton_Click(object sender, RoutedEventArgs e)
        {
            await SenseHelper.LaunchLocationSettingsAsync();
            // Although asynchronous, this completes before the user has actually done anything.
            // The application will loose control, the system settings will be displayed.
            // We will get the control back to our application via a visibilityChanged event.
        }
    }
}