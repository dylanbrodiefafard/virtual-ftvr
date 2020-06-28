
PlStream is an animation/script package that gives Unity
developers runtime access to 6DoF data captured by a Patriot,
Liberty, Fastrak or G4 tracking system. The package contains:
- PlStream.cs, a C# script 
- PlSimple, an example scene, and
- UnityExport.exe, an executable with supporting libraries.

UnityExport connects to whichever tracking system you
possess, opens a UDP socket and exports the raw data to that
socket. PlStream.cs receives this data and parses it to a common
interface, found in the PlStream class.

Usage:
Import the PlStream.unitypackage Asset into your Unity project.

Power on your Liberty, G4, Patriot or Fastrak.
	NOTE: Only one tracker may be running at a time.

In the Project Tab, open Assets->PlStream.
Right click on 'UnityExport' and select 'Show in Explorer'.
	Launch the UnityExport executable.
	
Attach the script PlStream.cs to an object.
Configure the script:
	Select your tracking device
	Select the number of systems you are using (G4)
	Select the maximum number of sensors per system (Liberty)
	Select your port. This is 5123 for UnityExport

To run the sample scene Plsimple:
Open Assets->PlStream->_Scenes->PlSimple.
In the Hierarchy Tab, see all of the objects in the PlSimple scene.
Select the Main Camera object.    
The Inspector Tab displays the components attached to the Main Camera object.
The PlStream script is attached to this object.
Configure the PlStream script:
	Port : This is always 5123.  Do not change this.
	Select Tracker_type: Liberty/Patriot/G4/Fastrak
	Select Max_systems:  
		For Liberty/Patriot/Fastrak: Max_Systems *must* be 1.
		For G4: Max_Systems is the number of G4 Hubs in your setup.
	Select Max_sensors:
		For Liberty: Max_sensors = 4,8,12, or 16, depending on your Liberty Hardware.
		For Patriot: Max_sensors = 2
		For Fastrak: Max_sensors = 4
		For G4:      Max_sensors = 3

	
Only a single PlStream may be active in a project at any time.
You can attach it to an empty object and link to it in the setup
phase of any objects in the scene you'd like to consume the data
with.

Important Notes 
About Using G4 with PlStream:
1. The UnityExport utility program uses a fixed file name "def_posx.g4c" 
    to start a G4 session.  The G4C Source Configuration file that you have 
    configured for your G4 tracking setup should be copied into the PlStream
    Assets folder and renamed to "def_posx.g4c"

    (If you are not familiar with the concept of the 
    G4C file and the G4 session, please first review the G4 User Manual and
    run the G4 system with the Polhemus PiMgr program.)

2.  The UnityExport utility does not have the ability to set G4 tracker 
    configuration parameters.   When used with Unity, the G4 tracking system
    can only be used with default settings.

About Using Liberty/Patriot/FasTrak:
1.  The PlStream and UnityExport combination has no mechanism for setting tracker
    configuration.  To use non-default configurations, you must first set up
    the tracker with the settings you require and then save those settings as
    the default startup configuration.  Refer to your tracker's User Manual
    and to the PiMgr online help.

About building and running PlSimple:
1.  SimpleController.cs is a component of the PlSimple Main Camera object.  It searches
    for Knuckle objects by the "Knuckle" tag.  This tag is not exported with the 
    PlStream.unitypackage. 

    In order to build a working executable from the PlSimple scene, you must first create
    a tag called "Knuckle".  This is accomplished by doing the following:
        1. Open the Main Camera object in the Inspector. 
        2. Click on the Tag field at the top of the Inspector.  
        3. A menu pops up.  Select Add Tag..
        4. The Inspector Tags & Layers pane appears.  
        5. Under the Tags section, click on the "+". 
        6. Type "Knuckle" in the new tag field.

Version History:
~ 1.0.0 2016/03/22 ~
Initial Release