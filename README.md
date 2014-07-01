
Activities
==========

Activities is a simple application demonstrating the use of the Activity Monitor,
one of the four APIs offered by the Lumia SensorCore SDK.

With the phone collecting in the background information about user's activity the
application is able to access the recorded activities and build a statistic of the
types of activities the user has performed during the current day (from 00:00 to
current time). This data will be displayed as a list of activities and their
total duration.

The application will also register for receiving activity change notifications
from the Lumia SensorCore and will display the change dynamically.


1. Instructions
--------------------------------------------------------------------------------

Learn about the Lumia SensorCore SDK from the Lumia Developer's Library. The
example requires the Lumia SensorCore SDK's NuGet package but will retrieve it
automatically (if missing) on first build.

To build the application you need to have Windows 8.1 and Windows Phone SDK 8.1
installed.

Using the Windows Phone 8.1 SDK:

1. Open the SLN file: File > Open Project, select the file `activities.sln`
2. Remove the "AnyCPU" configuration (not supported by the Lumia SensorCore SDK)
or simply select ARM
3. Select the target 'Device'.
4. Press F5 to build the project and run it on the device.

Alternatively you can also build the example for the emulator (x86) in which case
the Activity Monitor will use simulated data (and no history is available with
the default constructor used).

Please see the official documentation for
deploying and testing applications on Windows Phone devices:
http://msdn.microsoft.com/en-us/library/gg588378%28v=vs.92%29.aspx


2. Implementation
--------------------------------------------------------------------------------

**Important files and classes:**

The core of this app's implementation is in MainPage.xaml.cs where the Activity
Monitor API is initialized (if supported) with its production implementation
ActivityMonitor() when the app runs on a real device or with is simulated
alternative ActivityMonitorSimulator() when running on emulator.

The API is called through the CallSensorCoreApiAsync () helper function, which
helps handling the typical errors, like required features being disabled in the
system settings.

The data read via the API is handled in the MyData class which also has an mock
implementation (MyDesignData) used in IDE's design mode.

**Required capabilities:**

The SensorSore SDK (via its NuGet package) automatically inserts in the manifest
file the capabilities required for it to work:

    <DeviceCapability Name="location" />
    <m2:DeviceCapability Name="humaninterfacedevice">
      <m2:Device Id="vidpid:0421 0716">
        <m2:Function Type="usage:ffaa 0001" />
        <m2:Function Type="usage:ffee 0001" />
        <m2:Function Type="usage:ffee 0002" />
        <m2:Function Type="usage:ffee 0003" />
        <m2:Function Type="usage:ffee 0004" />
      </m2:Device>
    </m2:DeviceCapability>
	
	
3. License
--------------------------------------------------------------------------------

See the license text file delivered with this project. The license file is also
available online at https://github.com/nokia-developer/activities/blob/master/Licence.txt


4. Version history
--------------------------------------------------------------------------------

* Version 1.0: The first release.


5. See also
--------------------------------------------------------------------------------

The projects listed below are exemplifying the usage of the SensorCore APIs

* Steps -  https://github.com/nokia-developer/steps
* Places - https://github.com/nokia-developer/places
* Tracks - https://github.com/nokia-developer/tracks
* Activities - https://github.com/nokia-developer/activities
* Recorder - https://github.com/nokia-developer/recorder
