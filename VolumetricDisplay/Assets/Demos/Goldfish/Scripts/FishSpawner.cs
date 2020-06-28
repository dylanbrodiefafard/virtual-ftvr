using UnityEngine;

using Random = UnityEngine.Random;

public class FishSpawner : MonoBehaviour
{
    [Range(1, 200)]
    public int NumberOfFish = 15;

    [Range(0.01F, 1.0F)]
    public float FishSizeVariance = 0.5F;

    public GameObject FishPrefab;

    private WanderingBounds Bounds;

    public void TellHidingToGoHome()
    {
        // Tells each existing AI to "go home"
        foreach (var ai in FindObjectsOfType<HidingAI>())
        {
            ai.GoHome();
        }
    }

    private void Start()
    {
        // 
        Bounds = GetComponent<WanderingBounds>();

        // 
        for (int i = 0; i < NumberOfFish; i++)
        {
            var fish = Instantiate(FishPrefab, transform);
            fish.name = string.Format("Fish {0}", i);

            var fish_Transform = fish.GetComponent<Transform>();
            fish_Transform.localScale = Vector3.one * (1F - Random.Range(0, FishSizeVariance));
            fish_Transform.position = Bounds.GetRandomLocationInBounds(Vector3.one);

            var fish_Behaviour = fish.GetComponent<FishBehaviour>();
            fish_Behaviour.WanderingBounds = Bounds;
        }
    }
}
