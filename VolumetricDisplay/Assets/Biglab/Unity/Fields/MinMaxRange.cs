using System;
using Biglab.Extensions;
using UnityEngine;

using Random = UnityEngine.Random;

[Serializable]
public class MinMaxRange
{
    public float Min = 0F;

    public float Max = 1F;

    public MinMaxRange(float min, float max)
    {
        Min = min;
        Max = max;
    }

    public void Sort()
    {
        // Max was smaller, swap
        if (Max < Min)
        {
            var tmp = Min;
            Min = Max;
            Max = tmp;
        }
    }

    public float GetRandomValue()
    {
        return Random.Range(Min, Max);
    }

    public float GetValue(float t)
    {
        return Mathf.Lerp(Min, Max, t);
    }
}
