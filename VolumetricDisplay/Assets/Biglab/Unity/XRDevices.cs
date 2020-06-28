using UnityEngine.XR;

public static class XRDevices
{
    public enum Family
    {
        Oculus,
        Vive,
        DesktopStereo,
        Other
    }

    public const string ViveFamilyKeyword = "vive";
    public const string OculusFamilyKeyword = "oculus";
    public const string DesktopStereoFamilyKeyword = "split";

    public static Family ActiveFamily
    {
        get
        {
            var model = XRDevice.model;

            if (model.ToLower().Contains(ViveFamilyKeyword))
            {
                // Must be a Vive headset.
                return Family.Vive;
            }
            else if (model.ToLower().Contains(OculusFamilyKeyword))
            {
                // Must be an Oculus headset.
                return Family.Oculus;
            }
            else if (model.Contains(DesktopStereoFamilyKeyword))
            {
                // Must be desktop stereo
                return Family.DesktopStereo;
            }
            else
            {
                // Must be a Windows VR headset. (or other?? //)
                return Family.Other;
            }
        }
    }
}