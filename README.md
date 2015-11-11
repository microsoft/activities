
Activities
==========

Activities is a simple application demonstrating the use of the Activity Monitor,
one of the four APIs offered by the Lumia SensorCore SDK. Starting with Windows 10,
Activity Sensor support has been added to Windows.Devices.Sensors namespace.
This sample checks if Activity Sensor is exposed through the underlying OS APIs in 
Windows.Devices.Sensors before trying to use the one exposed by Lumia SensorCore SDK.

With the phone collecting background information about user's activity the
application is able to access the recorded activities and build a statistic of the
types of activities the user has performed during the current day (from 00:00 to
current time). This data will be displayed as a list of activities and their
total duration.

The application will also register for receiving activity change notifications
from the Lumia SensorCore and will display the change dynamically.

1. Instructions
--------------------------------------------------------------------------------

Learn about the Lumia SensorCore SDK from the Lumia Developer's Library. The
example requires the Lumia SensorCore SDK's NuGet package.

To build the application you need to have Windows 10 and Windows 10 SDK
installed.

Using the Windows 10 SDK:

1. Open the SLN file: File > Open Project, select the file `activities.sln`
2. Remove the "AnyCPU" configuration (not supported by the Lumia SensorCore SDK)
or simply select ARM
3. Select the target 'Device'.
4. Press F5 to build the project and run it on the device.

Alternatively you can also build the example for the emulator (x86) in which case
the Activity Monitor will use simulated data (and no history is available with
the default constructor used).

Please refer to the official SDK sample documentation for Universal Windows Platform
development. https://github.com/microsoft/windows-universal-samples/


2. Implementation
--------------------------------------------------------------------------------

**Important files and classes:**

The core of this app's implementation is in MainPage.xaml.cs where it opens and 
initializes the activity sensor instance when the page is loaded.

The main page does not directly talk to the SensorCore API but a wrapper in 
ActivitySensor.cs. The wrapper tries to use ActivitySensor exposed through Windows.
Devices.Sensors before falling back to production implementation ActivityMonitor()
when the app runs on a real device or with its simulated alternative 
ActivityMonitorSimulator() when running on emulator.

All APIs are called through the CallSensorCoreApiAsync () helper function, which
helps handling the typical errors, like required features being disabled in the
system settings.

The data read via the API is handled in the MyData class which also has an mock
implementation (MyDesignData) used in IDE's design mode.

**Required capabilities:**

These are present by default in the manifest file

    <DeviceCapability Name="activity" />
    <DeviceCapability Name="location" />
    <DeviceCapability Name="humaninterfacedevice">
      <Device Id="vidpid:0421 0716">
        <Function Type="usage:ffaa 0001" />
        <Function Type="usage:ffee 0001" />
        <Function Type="usage:ffee 0002" />
        <Function Type="usage:ffee 0003" />
        <Function Type="usage:ffee 0004" />
      </Device>
    </DeviceCapability>
	
	
3. Version history
--------------------------------------------------------------------------------
* Version 2.0:
  * Refactoring the sample to use ActivitySensor from Windows.Devices.Sensors namespace
    (if it's available). The sample will fallback to SensorCore if there is no 
	ActivitySensor surfaced by the OS.
* Version 1.1.0.17: 
  * Updated to use latest Lumia SensorCore SDK 1.1 Preview
* Version 1.1.0.13: 
 * Some bug fixes made in this release. 
* Version 1.0: The first release.

4. Downloads
---------

| Project | Release | Download |
| ------- | --------| -------- |
| Activities | v2.0 | [activities-2.0.zip](https://github.com/Microsoft/activities/archive/v2.0.zip) |
| Activities | v1.1.0.17 | [activities-1.1.0.17.zip](https://github.com/Microsoft/activities/archive/v1.1.0.17.zip) |
| Activities | v1.1.0.13 | [activities-1.1.0.13.zip](https://github.com/Microsoft/activities/archive/v1.1.0.13.zip) |
| Activities | v1.0 | [activities-1.0.zip](https://github.com/Microsoft/activities/archive/v1.0.zip) |

5. See also
--------------------------------------------------------------------------------

The projects listed below are exemplifying the usage of the SensorCore APIs

* Steps -  https://github.com/Microsoft/steps
* Places - https://github.com/Microsoft/places
* Tracks - https://github.com/Microsoft/tracks
* Activities - https://github.com/Microsoft/activities
* Recorder - https://github.com/Microsoft/recorder
