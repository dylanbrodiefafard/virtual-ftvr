using UnityEngine;

namespace Biglab.Displays.Virtual
{
    public class VirtualDisplaySubsystemSpawner : MonoBehaviour
    {
        private void Awake()
        {
            if (Config.VirtualRenderer.UseFastRendering)
            {
                gameObject.AddComponent<PerspectiveProjectorRenderer>();
            }
            else
            {
                gameObject.AddComponent<BiglabProjectorRenderer>();
            }

            Destroy(this);
        }
    }
}