# virtual-ftvr
A Virtual Testbed for Fish-Tank Virtual Reality.

This repository contains a [Unity](https://unity.com/) project that is capable of rendering 3D scenes to virtual or physical fish-tank VR (FTVR) display. These types of displays use perspective-corrected rendering to achieve a 3D effect. A stereo-capable rendering pipeline has also been included for stereoscopic rendering.

# Installation
The source code in this project was last developed and compiled with the following versions of tools and is known to work with these. Compatibility cannot be guaranteed if you use newer versions of these tools.
## Separate Installation
Download and install Unity and Visual Studio individually.
* Install Visual Studio 2017 - Archive located [here](https://visualstudio.microsoft.com/vs/older-downloads/)
* Install Unity 2018.1.7f1 - Archive located [here](https://unity3d.com/get-unity/download/archive)
* Launch Unity and open the `VolumetricDisplay` folder as the project
## Unity Hub
Visual Studio Community Edition can be included with the installation of Unity using this tool.

* Download and install the Unity Hub installation tool from [here](https://unity3d.com/get-unity/download)
* Go to the Unity archive [here](https://unity3d.com/get-unity/download/archive) and click on the Unity Hub [link](unityhub://2018.1.7f1/4cb482063d12) for Unity 2018.1.7
* Once the installation is complete, add the `VolumetricDisplay` folder to Projects.

# Volumetric Displays - Configuration File

Upon first running an application built using our volumetric display code, it will automatically generate a `config.json` file. This file contains individual settings for the start-up state of the application. If running from the Unity editor, you can find this file in the project root directory (`VolumetricDisplay` folder).

At the time of writing, this is what it looks like:
```json
{
    "CalibrationPath": "CalibrationFiles",
    "RemoteViewer": {
        "Port": 32032,
        "Address": "192.168.0.161"
    },
    "GeneralDisplay": {
        "Type": "mosaic"
    },
    "GeneralTracker": {
        "Type": "optitrack",
        "ScalingFactor": 1.0,
        "Mapping": [
         0,3
        ]
    },
    "MosaicRenderer": {
        "Width": 1024,
        "Height": 768,
        "Count": 4,
        "Radius": 0.30480000376701357,
        "ProjectorMapping": [
            3,
            1,
            0,
            2
        ]
    },
    "VirtualRenderer": {
        "DisplayName": "sphere",
        "UseFastRendering": true
    },
    "Polhemus": {
        "TrackerType": "patriot",
        "MaxSensors": 2,
        "MaxSystems": 1,
        "ChangeHandedness": false
    }
}
```

In the root level of the file there are *general* and *specific* configurations.

For *general* configurations:

* `CalibrationPath` - The path to a folder with display calibration data ( `pro1pixel_.bin`, etc ).
* `RemoteViewer` - Options for the companion app.
* `GeneralDisplay` - Options for which *display subsystem* to use.
    * Type -  Options include: `mosaic`, `virtual` or  `calibration`.
        * `mosaic` - The standard display output using N projectors mapped to a single mosaic display.
        * `calibration` - Using display calibration data, reconstructs the surface of the display.
        * `virtual` - A completely virtual option useful for viewing within Unity editor or desktop builds. *This mode doesn't require any real calibration data as it will generate its own synthetic data.*
* `GeneralTracker` - Options for which *tracking subsystem* to use.
    * Type -  Options include: `optitrack`, `polhemus` or `virtual`.
        * `optitrack` - Using Motive to stream Optitrack data.
        * `polhemus` - Using a Polhemus tracker such as Patriot.
        * `virtual` - Using a virtual tracking option.
            * In VR, the tracking points have a mapping to the headset and controllers.
            * On desktop the first tracking object is tied to the orbiting camera and controlled with a mouse.
    * ScalingFactor - Answers the question "How many tracking units per meter?".
    * Mapping - Maps the slots on the tracking technology to an object number within Unity.
        * For example: Optitrack can map 1-to-1 ( as seen above ) while Polhemus may have only 2 slots but suppose we desire to map slots 0 and 1 to be objects 0 and 3 ( ex, `"Mapping": [0,3]` ).

For *specific* configurations:

* `MosaicRenderer` - Settings about the *mosaic rendering subsystem*.
    * Width & Height - Dimensions of one of the N projectors.
    * Count - The number of projectors.
    * Radius - The radius of the Sphere in meters.
    * ProjectorMapping - A software level reordering of the projectors.
* `VirtualRenderer` - Settings about the *virtual rendering subsystem*.
    * DisplayName - Options include: `sphere`.
    * UseFastRendering - A toggle between nearly complete virtual simulation of the physical display ( slower ) using virtual projectors or an approximate simulation ( faster ).
* `Polhemus` - Settings about the *polhemus tracking subsystem*.
    * TrackerType - Options include: `liberty`, `patriot`, `g4`, `fastrak`
    * MaxSensors - The number of slots available in a single system.
    * MaxSystems - The max number of systems possible. Note: Should be set to 1 except for `g4` which may support more than 1.
    * ChangeHandedness - A toggle ( which can be manipulated at runtime ) to switch between a LH and RH coordinate system.

# Unity Project Hierarchy
All of the source code is located in the `Assets` folder. Important folders are:
* `Biglab` - Source code developed at [BIGLAB](https://biglab.ca) from University of Saskatchewan.
    * `Calibration` - All code related to the calibration of FTVR displays and projectors.
    * `Remote` - All code related to the remote (Android) control app.
    * `Scenes` - Calibration, debug, or setup scenes.
    * `Shaders` - All shader programs related to rendering FTVR content.
    * `Tracking` - All code related to tracking systems.
* `Demos` - Working demos.
* `OptimizationTest` - A debugging scene for the calibration optimization.
* `VirtualStudy` - All code related to CHI 2019 paper *FTVR in
VR: evaluating 3D performance with a simulated volumetric fish-tank virtual reality display.*

# Setting up a new scene
You can look at scenes in `Assets/Demos` or `Biglab/Scenes` for examples. 
## Simple setup
* Create a new scene
* Right click in the Hierarchy window and add
    * Biglab > `Volumetric Camera`
    * Biglab > Viewers > `Perspective Viewer (Primary)`
* Create your scene however you would like
* Translate, rotate, and scale the volumetric camera to fit your scene
* Because rendering of the FTVR display occurs in real-time, you can add controls to the `Volumetric Camera` to move, rotate, or scale it.

## Configure your project
### Virtual desktop rendering
* Set the "GeneralDisplay": "Type" and "GeneralTracker": "Type" to "virtual" in the config file.
* Go to Edit > Project Settings > Player and disable "Virtual Reality Supported"

*Note:* To enable 3D sterescopic rendering, Go to Edit > Project Settings > Player and enable "Virtual Reality Supported" and move "Stereo Display (non-head-mounted)" to the top of the "Virtual Reality SDKs" list. Your target display must support stereoscopic rendering.

### Oculus VR rendering
*Note:* You will need to install the [Oculus SDK](https://developer.oculus.com/downloads/unity/) for Unity to support this option.

* Set the "GeneralDisplay": "Type" and "GeneralTracker": "Type" to "virtual" in the config file.
* Go to Edit > Project Settings > Player and enable "Virtual Reality Supported" and move "Oculus" to the top of the "Virtual Reality SDKs" list.

### Physical FTVR rendering
* Set the "GeneralDisplay": "Type" to "mosaic" "GeneralTracker": "Type" to the tracker of your choice.

To support this display mode, your displays must be setup as a mosiac display and you must provide calibration data. This project is not capable of generating calibration data for physical FTVR displays. For more information about the calibration of these displays, contact the maintainer of this repository or the authors of [10.1109/WACV.2017.124](https://doi.org/10.1109/WACV.2017.124).

See [here](https://www.nvidia.com/en-us/design-visualization/solutions/nvidia-mosaic-technology/) for more details on mosaic displays.



*Note:* To support 3D sterescopic rendering, Go to Edit > Project Settings > Player and enable "Virtual Reality Supported" and move "Stereo Display (non-head-mounted)" to the top of the "Virtual Reality SDKs" list. Your display must support stereoscopic rendering.

# Remote App
The Remote App is an Android application that can be built and deployed to compatible Android devices that is capable of controlling the Unity project. It can be configured to change scenes, interact with menus, or stream content to the device.

Not a lot of documentation exists for this feature, but to compile it you must change the build target to Android and include only the `Biglab/Scenes/Remote Client` scene. Once the `.apk` is built you can install it on compatible Android devices.

# Third-party Licenses
## ALGLIB 3.14.0 (source code generated 2018-06-16)
GNU General Public License version 2 (GPLv2)  - You can find the included license [here](VolumetricDisplay/Assets/ALGLIB/alglib_info.cs).

Visit the [FAQ](https://www.alglib.net/faq.php) for more details on licensing and this library.

# Publications
More details on the design and implementation of this source code can be found in the following publications.

* Dylan Fafard, Ian Stavness, Martin Dechant, Regan Mandryk, Qian Zhou, and Sidney Fels. FTVR in
VR: evaluating 3D performance with a simulated volumetric fish-tank virtual reality display. In CHI
Conference on Human Factors in Computing Systems Proceedings (CHI 2019), 2019. [Link](https://dl.acm.org/doi/pdf/10.1145/3290605.3300763)
* Dylan Fafard, Qian Zhou, Georg Hagemann, Chris Chamberlain, Sidney Fels, and Ian Stavness. Design
and implementation of a multi-person fish-tank virtual reality display. In ACM Symposium on Virtual
Reality Software and Technology, 2018. [Link](https://dl.acm.org/doi/pdf/10.1145/3281505.3281540)
* Dylan Brodie Fafard. A Virtual Testbed for Fish-Tank Virtual Reality: Improving Calibration with a Virtual-in-Virtual Display. University of Saskatchewan Electronic Theses and Dissertations, 2019. [Link](http://hdl.handle.net/10388/12132)
