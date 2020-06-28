using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Biglab.Extensions
{
    public static class TransformExtension
    {
        public static IEnumerable<Transform> GetChildren(this Transform @this)
        {
            foreach (Transform child in @this)
            {
                yield return child;
            }
        }

        //Breadth-first search
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            var result = aParent.Find(aName);
            if (result != null)
            {
                return result;
            }

            foreach (Transform child in aParent)
            {
                result = child.FindDeepChild(aName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public static Transform FindChild(this Transform parent, Predicate<Transform> predicate)
        {
            return FindChildren(parent, predicate).FirstOrDefault();
        }

        public static IEnumerable<Transform> FindChildren(this Transform parent, Predicate<Transform> predicate)
        {
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (predicate(child))
                {
                    yield return child;
                }
            }
        }
    }
}