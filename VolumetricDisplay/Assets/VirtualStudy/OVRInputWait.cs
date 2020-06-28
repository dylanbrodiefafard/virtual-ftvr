using System.Collections;
using UnityEngine;

public class OVRInputWait : SingletonMonobehaviour<OVRInputWait>
{
    /// <summary>
    /// Wait for any button up event from OVRInput
    /// </summary>
    public IEnumerator AnyButtonUp
    {
        get
        {
            yield return new WaitForEndOfFrame(); 
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(() => OVRInput.GetUp(OVRInput.Button.Any));
        }
    }
}
