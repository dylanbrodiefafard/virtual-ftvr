using System;
using System.Linq;

using UnityEngine;

public class PhoneCamera : MonoBehaviour
{
    /// <summary>
    /// Is the camera available?
    /// </summary>
    public bool IsCameraAvailable => _cameraTexture != null;

    /// <summary>
    /// Has the camera been started and updating the texture?
    /// </summary>
    public bool HasCameraStarted => IsCameraAvailable && _cameraTexture.isPlaying;

    /// <summary>
    /// The scaling factor accounting for vertical flip.
    /// </summary>
    public float TextureScaleVerticalFlip
    {
        get
        {
            if (IsCameraAvailable)
            {
                // Vertical Flipping
                return _cameraTexture.videoVerticallyMirrored ? -1F : +1F;
            }
            else
            {
                return 1F;
            }
        }
    }

    /// <summary>
    /// Rotational angle for device orientation.
    /// </summary>
    public int TextureOrientation
    {
        get
        {
            if (IsCameraAvailable)
            {
                return -_cameraTexture.videoRotationAngle;
            }
            else
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// The webcam texture.
    /// </summary>
    public Texture Texture => _cameraTexture;

    [SerializeField, ReadOnly]
    private WebCamTexture _cameraTexture;

    private static readonly string[] _badCameraNames = new[]
    {
        // Leap Motion Controller
        "leap", 
        // Point Grey Camera
        "point grey"
    };

    private void Awake()
    {
        var devices = WebCamTexture.devices.Where(FilterKnownBadCameras);

        // No cameras
        if (!devices.Any())
        {
            // No camera
            Debug.Log("No camera detected.");
            return;
        }

        // Log which cameras are found
        Debug.Log($"Found: {devices.Count()} cameras.");
        foreach (var device in devices)
        {
            Debug.Log($"Camera: {device.name} ( {(device.isFrontFacing ? "Front" : "Back")} ).");
        }

        // Find a webcam texture, prioritizing back facing cameras
        foreach (var device in devices.OrderBy(dev => !dev.isFrontFacing))
        {
            Debug.Log($"Using Camera: {device.name} ( {(device.isFrontFacing ? "Front" : "Back")} ).");
            _cameraTexture = new WebCamTexture(device.name);
            continue;
        }
    }

    /// <summary>
    /// Start updating the texture with the camera feed.
    /// </summary>
    public void StartCamera()
    {
        try
        {
            if (IsCameraAvailable && _cameraTexture.isPlaying == false)
            {
                _cameraTexture.Play();
            }
        }
        catch (Exception)
        {
            // Failed to play, clear camera
            Debug.Log($"Unable to use camera: {_cameraTexture.deviceName}. ( Unable to open )");
            _cameraTexture = null;
        }
    }

    /// <summary>
    /// Stop updating the texture with the camera feed.
    /// </summary>
    public void StopCamera()
    {
        if (IsCameraAvailable && _cameraTexture.isPlaying)
        {
            _cameraTexture.Stop();
        }
    }

    private bool FilterKnownBadCameras(WebCamDevice device)
    {
        var name = device.name.ToLowerInvariant();

        // Skips "Leap Motion Controller" or "Point Grey Camera" for example.
        foreach (var badName in _badCameraNames)
        {
            if (name.Contains(badName))
            {
                // Camera was on the black list
                return false;
            }
        }

        // Camera not known, assume its good to use
        return true;
    }
}
