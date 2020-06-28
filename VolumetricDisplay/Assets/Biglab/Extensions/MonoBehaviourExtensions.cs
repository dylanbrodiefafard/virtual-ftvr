using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biglab.Extensions
{
    public static class MonoBehaviourExtensions
    {
        /// <summary>
        /// Finds the first available component of type <typeparamref name="TComponent"/>, if the given reference is null.
        /// </summary>
        public static void FindComponentReference<TComponent>(this Behaviour @this, ref TComponent obj)
            where TComponent : Component
        {
            if (obj == null)
            {
                obj = @this.GetComponent<TComponent>();
            }

            if (obj == null)
            {
                obj = Object.FindObjectOfType<TComponent>();
            }

            if (obj == null)
            {
                Debug.LogError($"{typeof(TComponent)} object not found.");
                @this.enabled = false;
            }
        }

        /// <summary>
        /// Finds the first available component of type <typeparamref name="TComponent"/>.
        /// </summary>
        public static TComponent FindComponentReference<TComponent>(this Behaviour @this) where TComponent : Component
        {
            TComponent obj = null;
            FindComponentReference(@this, ref obj);
            return obj;
        }

        /// <summary>
        /// Finds the first available component of type <typeparamref name="TComponent"/>, creating it if missing.
        /// </summary>
        public static void FindOrCreateComponentReference<TComponent>(this Behaviour @this, ref TComponent obj,
            bool parent = false) where TComponent : Component
        {
            if (obj == null)
            {
                obj = @this.GetComponent<TComponent>();
            }

            if (obj == null)
            {
                obj = Object.FindObjectOfType<TComponent>();
            }

            if (obj == null)
            {
                // TODO: Should this create an object or add it to the current object?
                var go = new GameObject(typeof(TComponent).Name);
                // go.transform.SetParent(@this.transform, false);
                obj = go.AddComponent<TComponent>();
            }
        }

        /// <summary>
        /// Finds the first available component of type <typeparamref name="TComponent"/>, creating it if missing.
        /// </summary>
        public static TComponent FindOrCreateComponentReference<TComponent>(this Behaviour @this)
            where TComponent : Component
        {
            TComponent obj = null;
            FindOrCreateComponentReference(@this, ref obj);
            return obj;
        }

        /// <summary>
        /// Waits a frame (with a coroutine) and then executes the given action.
        /// </summary>
        public static void WaitOneFrame(this MonoBehaviour @this, Action action)
        {
            if (Application.isPlaying)
            {
                @this.StartCoroutine(WaitOneFrame_Coroutine(action));
            }
            else
            {
                action();
            }
        }

        private static IEnumerator WaitOneFrame_Coroutine(Action action)
        {
            yield return null;
            action();
        }
    }
}