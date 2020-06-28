using System;
using System.IO;
using System.Linq;
using System.Net;
using Biglab.Displays;
using Biglab.Displays.Virtual;
using Biglab.Interoperability;
using Biglab.IO.Networking;
using Biglab.IO.Serialization;
using Biglab.Tracking;
using UnityEngine;

namespace Biglab
{
    /// <summary>
    /// Contains runtime configuration information set in a configuration file.
    /// </summary>
    public static class Config
    {
        #region Constants

        private const string _configurationFile = "config.json";

        // REMOTE VIEWER

        private const int _defaultRemoteViewerPort = 32032;

        private const int _defaultRemoteViewerWidth = 640;

        private const int _defaultRemoteViewerHeight = 360;

        private const int _defaultRemoteViewerQuality = 66;

        private static readonly Vector3 _defaultViewInDevice = new Vector3(0, 0, -0.4f);

        private const float _defaultDeviceDiagonal = 0.2286f; // 9 inches in meters

        private const float _defaultDeviceScreenAspect = 1.3333333333333333333333333333333f; // 4 : 3 aspect ratio

        // GENERAL DISPLAY

        private const string _defaultDisplayType = "virtual";

        private static readonly string[] _validDisplayTypes =
        {
            "debug", "mosaic", "virtual", "calibration"
        };

        // MOSAIC RENDERER
        private const int _defaultProjectorWidth = 1024;

        private const int _defaultProjectorHeight = 768;

        private const int _defaultProjectorCount = 4;

        private const float _defaultRadius = 0.3048F; // 12 inches in meters

        private static readonly int[] _defaultProjectorMapping = { 0, 1, 2, 3 };

        // VIRTUAL RENDERER

        private const string _defaultVirtualDisplayName = "sphere";

        private const bool _defaultVirtualUseFastRendering = true;

        private static readonly string[] _validVirtualDisplayNames = { "sphere", "bunny", "pyramid", "cube" };

        // TRACKER

        private const int _numberOfTrackingObjects = 8; // DF: Where did this number come from?

        private const float _defaultTrackerScalingFactor = 1F;

        private const string _defaultTrackerType = "virtual";

        private static readonly string[] _validTrackerTypes = { "optitrack", "polhemus", "virtual", "fixed" };

        private const string _defaultCalibrationPath = "CalibrationFiles";

        // POLHEMUS

        private const string _defaultPolhemusTracker = "patriot";

        private static readonly string[] _validPolhemusTrackers = { "liberty", "patriot", "g4", "fastrak" };

        private const int _defaultPolhemusMaxSensors = 2;

        private const int _defaultPolhemusMaxSystems = 1;

        private const bool _defaultPolhemusChangeHandedness = false;

        // Viewpoint Solver

        private const int _viewpointSolverNumberOfParameters = 9;

        private static readonly double[] _defaultViewpointSolverLowerBounds = { -1, -1, -1, -1, -10, -10, -10, -0.15, -0.15 };

        private static readonly double[] _defaultViewpointSolverUpperBounds = { +1, +1, +1, +1, +10, +10, +10, +0.15, +0.15 };

        private const double _defaultViewpointSolverXEpsilon = 0.00001;

        private const double _defaultViewpointSolverMaximumStep = 0.001;

        private const int _defaultViewpointSolverMaximumIterations = 750;

        #endregion Constants

        #region Public Properties

        /// <summary>
        /// The path to a directory containing calibration data.
        /// </summary>
        public static string CalibrationPath => _instance.CalibrationPath;

        /// <summary>
        /// Specific configuration for the remote viewer companion app.
        /// </summary>
        public static RemoteViewerConfig RemoteViewer => _instance.RemoteViewer;

        /// <summary>
        /// General configuration for the display and rendering.
        /// </summary>
        public static GeneralDisplayConfig GeneralDisplay => _instance.GeneralDisplay;

        /// <summary>
        /// General configuration for the tracker.
        /// </summary>
        public static GeneralTrackerConfig GeneralTracker => _instance.GeneralTracker;

        /// <summary>
        /// Specific configuration for the mosaic renderering display mode.
        /// </summary>
        public static MosaicRendererConfig MosaicRenderer => _instance.MosaicRenderer;

        /// <summary>
        /// Specific configuration for the virtual renderering display mode.
        /// </summary>
        public static VirtualRendererConfig VirtualRenderer => _instance.VirtualRenderer;

        /// <summary>
        /// Specific configuration for Polhemus tracker devices.
        /// </summary>
        public static PolhemusConfig Polhemus => _instance.Polhemus;

        /// <summary>
        /// Specific configuration for viewpoint optimization.
        /// </summary>
        public static ViewpointOptimizationConfig ViewpointOptimization => _instance.ViewpointOptimization;

        #endregion Public Properties

        #region Load/Save/Default

        private static readonly ConfigFile _instance;

        static Config()
        {
            if (File.Exists(_configurationFile))
            {
                // Read text
                var json = File.ReadAllText(_configurationFile);

                // Parse JSON
                _instance = json.DeserializeJson<ConfigFile>();
                _instance.Sanitize();

                // Save again ( to ensure missing fields, etc )
                Save();
            }
            else
            {
                // No file ( save initial default file )
                _instance = ConfigFile.CreateDefault();
                Save();
            }
        }

        public static void Save()
        {
            // Sanitize configuration
            _instance.Sanitize();

            // Encode as JSON ( pretty printed )
            File.WriteAllText(_configurationFile, _instance.SerializeJson(true));
        }

        #endregion Load/Save/Default

        public interface IConfig
        {
            void SetToDefault();

            void Sanitize();
        }

        #region Config File ( Top Level )

        [Serializable]
        private class ConfigFile : IConfig
        {
            public string CalibrationPath;
            public RemoteViewerConfig RemoteViewer;
            public GeneralDisplayConfig GeneralDisplay;
            public GeneralTrackerConfig GeneralTracker;
            public MosaicRendererConfig MosaicRenderer;
            public VirtualRendererConfig VirtualRenderer;
            public PolhemusConfig Polhemus;
            public ViewpointOptimizationConfig ViewpointOptimization;

            /// <summary>
            /// Attempts to keep the values reasonable to preventing erroneous like a null string.
            /// </summary>
            public void Sanitize()
            {
                if (CalibrationPath == null)
                {
                    CalibrationPath = _defaultCalibrationPath;
                }

                //
                if (!Directory.Exists(CalibrationPath))
                {
                    Debug.LogWarning($"Unable to find the calibration directory specified.\n\"{CalibrationPath}\" does not exist");
                }

                // Sanitize sub configurations
                GeneralDisplay.Sanitize();
                GeneralTracker.Sanitize();
                RemoteViewer.Sanitize();
                MosaicRenderer.Sanitize();
                VirtualRenderer.Sanitize();
                Polhemus.Sanitize();
                ViewpointOptimization.Sanitize();
            }

            public static ConfigFile CreateDefault()
            {
                var config = new ConfigFile();
                config.SetToDefault();
                return config;
            }

            public void SetToDefault()
            {
                CalibrationPath = _defaultCalibrationPath;
                RemoteViewer = RemoteViewerConfig.CreateDefault();
                GeneralTracker = GeneralTrackerConfig.CreateDefault();
                GeneralDisplay = GeneralDisplayConfig.CreateDefault();
                MosaicRenderer = MosaicRendererConfig.CreateDefault();
                VirtualRenderer = VirtualRendererConfig.CreateDefault();
                Polhemus = PolhemusConfig.CreateDefault();
                ViewpointOptimization = ViewpointOptimizationConfig.CreateDefault();
            }
        }

        #endregion Config File ( Top Level )

        #region General Display

        [Serializable]
        public class GeneralDisplayConfig : IConfig
        {
            public string Type;

            /// <summary>
            /// Attempts to get the display type enum from the string parameter.
            /// If ambiguous, returns as automatic.
            /// </summary>
            public DisplayType GetDisplayType()
            {
                switch (Type.ToLower())
                {
                    default:
                        return DisplayType.Virtual;

                    case "mosaic":
                        return DisplayType.Mosaic;

                    case "virtual":
                        return DisplayType.Virtual;

                    case "debug":
                    case "calibration":
                        return DisplayType.VirtualByCalibration;
                }
            }

            /// <summary>
            /// Creates some default
            /// </summary>
            public static GeneralDisplayConfig CreateDefault()
            {
                var config = new GeneralDisplayConfig();
                config.SetToDefault();
                return config;
            }

            public void Sanitize()
            {
                if (string.IsNullOrEmpty(Type) || !_validDisplayTypes.Contains(Type))
                {
                    Type = _defaultDisplayType;
                }
            }

            public void SetToDefault() => Type = _defaultDisplayType;
        }

        #endregion General Display

        #region Remote Viewer

        [Serializable]
        public class RemoteViewerConfig : IConfig
        {
            public int Port;
            public string Address;
            public int RenderWidth;
            public int RenderHeight;
            public int Quality;
            public float DeviceScreenDiagonal;
            public float DeviceScreenAspect;
            public Vector3 ViewInDevice;

            [NonSerialized]
            public Vector2 DeviceScreenSize;

            /// <summary>
            /// Creates some default
            /// </summary>
            public static RemoteViewerConfig CreateDefault()
            {
                var config = new RemoteViewerConfig();
                config.SetToDefault();
                return config;
            }

            public void Sanitize()
            {
                if (Port < 0 || Port >= ushort.MaxValue)
                {
                    Port = _defaultRemoteViewerPort;
                }

                //
                RenderWidth = Mathf.Min(Mathf.Max(RenderWidth, 170), 1920);
                RenderHeight = Mathf.Min(Mathf.Max(RenderHeight, 100), 1080);
                Quality = Mathf.Min(Mathf.Max(Quality, 1), 100);

                IPAddress address;
                if (string.IsNullOrWhiteSpace(Address) || !NetworkUtility.TryParseAddress(Address, out address))
                {
                    Address = NetworkUtility.GetLocalAddress().ToString();
                }

                DeviceScreenDiagonal = Mathf.Clamp(DeviceScreenDiagonal, 0.01f, 1);
                DeviceScreenAspect = Mathf.Clamp(DeviceScreenAspect, 0.1f, 10);

                DeviceScreenSize = new Vector2
                {
                    x = DeviceScreenAspect * Mathf.Sqrt(Mathf.Pow(DeviceScreenDiagonal, 2) / (Mathf.Pow(DeviceScreenAspect, 2) + 1)),
                    y = Mathf.Sqrt(Mathf.Pow(DeviceScreenDiagonal, 2) / (Mathf.Pow(DeviceScreenAspect, 2) + 1))
                };
            }

            public void SetToDefault()
            {
                Address = NetworkUtility.GetLocalAddress().ToString();

                RenderWidth = _defaultRemoteViewerWidth;
                RenderHeight = _defaultRemoteViewerHeight;
                Quality = _defaultRemoteViewerQuality;

                Port = _defaultRemoteViewerPort;

                ViewInDevice = _defaultViewInDevice;

                DeviceScreenDiagonal = _defaultDeviceDiagonal;
                DeviceScreenAspect = _defaultDeviceScreenAspect;

                DeviceScreenSize = new Vector2
                {
                    x = DeviceScreenAspect * Mathf.Sqrt(Mathf.Pow(DeviceScreenDiagonal, 2) / (Mathf.Pow(DeviceScreenAspect, 2) + 1)),
                    y = Mathf.Sqrt(Mathf.Pow(DeviceScreenDiagonal, 2) / (Mathf.Pow(DeviceScreenAspect, 2) + 1))
                };
            }
        }

        #endregion Remote Viewer

        #region Mosiac Renderer

        [Serializable]
        public class MosaicRendererConfig : IConfig
        {
            public int Width;

            public int Height;

            public int Count;

            public float Radius;

            public int[] ProjectorMapping;

            /// <summary>
            /// Creates some default
            /// </summary>
            public static MosaicRendererConfig CreateDefault()
            {
                var config = new MosaicRendererConfig();
                config.SetToDefault();
                return config;
            }

            public void Sanitize()
            {
                if (Width <= 0)
                {
                    Width = _defaultProjectorWidth;
                }

                if (Height <= 0)
                {
                    Height = _defaultProjectorHeight;
                }

                if (Count <= 0)
                {
                    Count = _defaultProjectorCount;
                }

                if (Radius <= 0)
                {
                    Radius = _defaultRadius;
                }

                if (ProjectorMapping == null || ProjectorMapping.Length != Count)
                {
                    ProjectorMapping = _defaultProjectorMapping;
                }
            }

            public void SetToDefault()
            {
                Width = _defaultProjectorWidth;
                Height = _defaultProjectorHeight;
                Count = _defaultProjectorCount;
                Radius = _defaultRadius;
            }
        }

        #endregion Mosiac Renderer

        #region Virtual Renderer

        [Serializable]
        public class VirtualRendererConfig : IConfig
        {
            public string DisplayName;

            public bool UseFastRendering;

            public VirtualDisplaySubsystem.DisplayType GetDisplayType()
            {
                switch (DisplayName.ToLower())
                {
                    case "sphere":
                        return VirtualDisplaySubsystem.DisplayType.Sphere;

                    default:
                        throw new InvalidOperationException($"Unable to determine display type from '{DisplayName}'");
                }
            }

            /// <summary>
            /// Creates some default
            /// </summary>
            public static VirtualRendererConfig CreateDefault()
            {
                var config = new VirtualRendererConfig();
                config.SetToDefault();
                return config;
            }

            public void Sanitize()
            {
                if (string.IsNullOrEmpty(DisplayName) || !_validVirtualDisplayNames.Contains(DisplayName.ToLower()))
                {
                    DisplayName = _defaultVirtualDisplayName;
                }
            }

            public void SetToDefault()
            {
                DisplayName = _defaultVirtualDisplayName;
                UseFastRendering = _defaultVirtualUseFastRendering;
            }
        }

        #endregion Virtual Renderer

        #region General Tracker

        [Serializable]
        public class GeneralTrackerConfig : IConfig
        {
            public string Type;

            public float ScalingFactor;

            // Maps Device Indices to Unity Indices
            // Ie, [0,2] says "device 0 to unity 0, device 1 to unity 2"
            public int[] Mapping;

            public GeneralTrackerConfig()
            {
                Mapping = new int[_numberOfTrackingObjects];
            }

            public TrackingType GetTrackerType()
            {
                switch (Type.ToLower())
                {
                    case "optitrack":
                        return TrackingType.Optitrack;

                    case "polhemus":
                        return TrackingType.Polhemus;

                    case "virtual":
                        return TrackingType.Virtual;

                    case "fixed":
                        return TrackingType.Fixed;

                    default:
                        throw new InvalidOperationException($"Unable to determine tracker type from '{Type}'");
                }
            }

            /// <summary>
            /// Is this tracker capable of 6 degree-of-freedom?
            /// </summary>
            public bool Is6DoF
            {
                get
                {
                    switch (GetTrackerType())
                    {
                        case TrackingType.Optitrack:
                            return true;

                        case TrackingType.Polhemus:
                            return true;

                        case TrackingType.Virtual:
                            return true;

                        case TrackingType.Fixed:
                            return false;

                        default:
                            Debug.LogWarning("Unknown tracker type, assuming 3DOF.");
                            return false;
                    }
                }
            }

            /// <summary>
            /// Creates some default tracker configuration.
            /// </summary>
            internal static GeneralTrackerConfig CreateDefault()
            {
                var config = new GeneralTrackerConfig();
                config.SetToDefault();
                return config;
            }

            public void Sanitize()
            {
                if (string.IsNullOrEmpty(Type) || !_validTrackerTypes.Contains(Type.ToLower()))
                {
                    Type = _defaultTrackerType;
                }

                // Resize
                if (Mapping.Length != _numberOfTrackingObjects)
                {
                    var oldLength = Mapping.Length;
                    Array.Resize(ref Mapping, _numberOfTrackingObjects);
                    if (oldLength < Mapping.Length)
                    {
                        for (var i = oldLength; i < Mapping.Length; i++)
                        {
                            Mapping[i] = i;
                        }
                    }
                }

                for (var i = 0; i < Mapping.Length; i++)
                {
                    if (Mapping[i] < 0)
                    {
                        Mapping[i] = i;
                    }
                }
            }

            public void SetToDefault()
            {
                // Default values
                Type = _defaultTrackerType;
                ScalingFactor = _defaultTrackerScalingFactor;

                // Enumerate 8 indices
                Mapping = new int[_numberOfTrackingObjects];
                for (var i = 0; i < Mapping.Length; i++)
                {
                    Mapping[i] = i;
                }
            }
        }

        #endregion General Tracker

        #region Polhemus

        [Serializable]
        public class PolhemusConfig : IConfig
        {
            public string TrackerType;

            public int MaxSensors;

            public int MaxSystems;

            public bool ChangeHandedness;

            /// <summary>
            /// Creates some default
            /// </summary>
            public static PolhemusConfig CreateDefault()
            {
                var config = new PolhemusConfig();
                config.SetToDefault();
                return config;
            }

            public PlTracker GetTrackerType()
            {
                switch (TrackerType.ToLower())
                {
                    case "patriot":
                        return PlTracker.Patriot;

                    case "fastrak":
                        return PlTracker.Fastrak;

                    case "liberty":
                        return PlTracker.Liberty;

                    case "g4":
                        return PlTracker.G4;

                    default:
                        throw new InvalidOperationException(
                            $"Unable to determine polhemus tracker type from '{TrackerType}'");
                }
            }

            public void Sanitize()
            {
                if (string.IsNullOrEmpty(TrackerType) || !_validPolhemusTrackers.Contains(TrackerType.ToLower()))
                {
                    TrackerType = _defaultPolhemusTracker;
                }

                // there are some constraints between tracking systems
                switch (GetTrackerType())
                {
                    case PlTracker.Liberty:
                        // liberty is a single tracker system
                        if (MaxSystems < 0 || MaxSystems > 1)
                        {
                            MaxSystems = _defaultPolhemusMaxSystems;
                        }

                        if (MaxSensors < 0 || MaxSensors > 16)
                        {
                            MaxSensors = _defaultPolhemusMaxSensors;
                        }

                        break;

                    case PlTracker.Patriot:
                        if (MaxSystems < 0 || MaxSystems > 1)
                        {
                            MaxSystems = _defaultPolhemusMaxSystems;
                        }

                        if (MaxSensors < 0 || MaxSensors > 2)
                        {
                            MaxSensors = _defaultPolhemusMaxSensors;
                        }

                        break;

                    case PlTracker.G4:
                        if (MaxSystems < 0 || MaxSystems > 10)
                        {
                            MaxSystems = _defaultPolhemusMaxSystems;
                        }

                        if (MaxSensors < 0 || MaxSensors > 3)
                        {
                            MaxSensors = _defaultPolhemusMaxSensors;
                        }

                        break;

                    case PlTracker.Fastrak:
                        if (MaxSystems < 0 || MaxSystems > 1)
                        {
                            MaxSystems = _defaultPolhemusMaxSystems;
                        }

                        if (MaxSensors < 0 || MaxSensors > 4)
                        {
                            MaxSensors = _defaultPolhemusMaxSensors;
                        }

                        break;
                }
            }

            public void SetToDefault()
            {
                TrackerType = _defaultPolhemusTracker;
                MaxSensors = _defaultPolhemusMaxSensors;
                MaxSystems = _defaultPolhemusMaxSystems;
                ChangeHandedness = _defaultPolhemusChangeHandedness;
            }
        }

        #endregion Polhemus

        #region Viewpoint Optimization

        [Serializable]
        public class ViewpointOptimizationConfig : IConfig
        {
            public double[] LowerBounds;

            public double[] UpperBounds;

            public double XEpsilon;

            public double MaximumStep;

            public int MaximumIterations;

            public ViewpointOptimizationConfig()
            {
                LowerBounds = new double[9];
                UpperBounds = new double[9];
            }

            public ALGLIB.SolverParameters GetParameters(double[] x0, int n)
                => new ALGLIB.SolverParameters
                {
                    lowerBounds = LowerBounds,
                    upperBounds = UpperBounds,
                    xEpsilon = XEpsilon,
                    maxStep = MaximumStep,
                    maxIterations = MaximumIterations,
                    x0 = x0,
                    n = n
                };

            internal static ViewpointOptimizationConfig CreateDefault()
            {
                var config = new ViewpointOptimizationConfig();
                config.SetToDefault();
                return config;
            }

            public void Sanitize()
            {
                if (LowerBounds.Length != _viewpointSolverNumberOfParameters)
                {
                    var oldLength = LowerBounds.Length;
                    Array.Resize(ref LowerBounds, _viewpointSolverNumberOfParameters);

                    for (var i = oldLength; i < LowerBounds.Length; i++)
                    {
                        LowerBounds[i] = _defaultViewpointSolverLowerBounds[i];
                    }
                }

                if (UpperBounds.Length != _viewpointSolverNumberOfParameters)
                {
                    var oldLength = UpperBounds.Length;
                    Array.Resize(ref UpperBounds, _viewpointSolverNumberOfParameters);

                    for (var i = oldLength; i < UpperBounds.Length; i++)
                    {
                        UpperBounds[i] = _defaultViewpointSolverUpperBounds[i];
                    }
                }

                if (MaximumIterations < 0)
                {
                    MaximumIterations = _defaultViewpointSolverMaximumIterations;
                }

                if (XEpsilon < 0)
                {
                    XEpsilon = _defaultViewpointSolverXEpsilon;
                }

                if (MaximumStep < 0)
                {
                    MaximumStep = _defaultViewpointSolverMaximumStep;
                }
            }

            public void SetToDefault()
            {
                LowerBounds = _defaultViewpointSolverLowerBounds;
                UpperBounds = _defaultViewpointSolverUpperBounds;
                XEpsilon = _defaultViewpointSolverXEpsilon;
                MaximumStep = _defaultViewpointSolverMaximumStep;
                MaximumIterations = _defaultViewpointSolverMaximumIterations;
            }
        }

        #endregion Viewpoint Optimization
    }
}