using System.Linq;

using UnityEngine;

public class HideAndSeekVictory : MonoBehaviour
{
    public GameObject VictoryBanner;

    public HidingAI[] Fish;

    public int Count;

    private void Start()
    {
        Fish = FindObjectsOfType<HidingAI>();
        Count = Fish.Length;
    }

    private void Update()
    {
        Count = Fish.Count(x => x);
        if (Count == 0 && !VictoryBanner.active)
        {
            VictoryBanner.SetActive(true);
            // Fanfare!
        }
    }
}
