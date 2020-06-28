using UnityEngine;

namespace Biglab.Extensions
{
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Adds a component but allows a configuration callback before Awake() is called.
        /// </summary>
        public static T AddComponentWithInit<T>(this GameObject obj, System.Action<T> onInit) where T : Component
        {
            var oldState = obj.activeSelf;
            obj.SetActive(false);
            var comp = obj.AddComponent<T>();
            onInit?.Invoke(comp);
            obj.SetActive(oldState);
            return comp;
        }

        /// <summary>
        /// Gets the first of the component on the game object. If the compoment cannot be found, adds the component to the game object instead.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject @this) where T : Component
        {
            var component = @this.gameObject.GetComponent<T>();

            if (component == null)
            {
                component = @this.gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}