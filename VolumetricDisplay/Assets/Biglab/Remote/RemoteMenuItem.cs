using System;

using Biglab.IO.Networking;
using Biglab.Remote.Client;
using Biglab.Utility;
using UnityEngine;
using UnityEngine.Events;

namespace Biglab.Remote
{
    public abstract class RemoteMenuItem : MonoBehaviour
    {
        /// <summary>
        /// The element itself ( serialized and sent to the remote client ).
        /// </summary>
        public abstract ElementData Data { get; }

        /// <summary>
        /// The grouping title ( groups sets of elements lexicographically ).
        /// </summary>
        public string Group
        {
            get { return Data.Group; }

            set
            {
                Data.Group = value;
                SyncElementData();
            }
        }

        /// <summary>
        /// Orders elements per group numerically.
        /// </summary>
        public int Order
        {
            get { return Data.Order; }

            set
            {
                Data.Order = value;
                SyncElementData();
            }
        }

        protected void SyncElementData()
            => RemoteSystem.Instance.UpdateElement(this);

        public abstract void HandleElementValueChange(INetworkConnection connection, object value);
    }

    public abstract class RemoteMenuItem<TUnityEvent, TElement, TValue> : RemoteMenuItem
        where TUnityEvent : UnityEvent<TValue, INetworkConnection>
        where TElement : ElementData, new()
    {
        [SerializeField]
        private TUnityEvent _onValueChanged;

        public event Action<TValue, INetworkConnection> ValueChanged;

        public override ElementData Data => Element;

        protected TElement Element => _element;

        [SerializeField]
        private TElement _element;

        #region MonoBehaviour

        protected virtual void OnEnable()
        {
            // Debug.Log($"Enabling - {name}:{GetType().Name}");

            RemoteSystem.Instance.RegisterElement(this);
        }

        protected virtual void OnDisable()
        {
            // Debug.Log($"Disabling - {name}:{GetType().Name}");

            if (RemoteSystem.Instance != null)
            {
                RemoteSystem.Instance.UnregisterElement(this);
            }
        }

        protected virtual void Reset()
            => _element = CreateElementData();

        protected virtual void Awake()
        {
            if (_element == null)
            {
                _element = CreateElementData();
            }
        }

        #endregion

        protected TElement CreateElementData()
        {
            var element = Activator.CreateInstance<TElement>();
            element.Group = gameObject.name;
            element.Id = GetInstanceID();
            return element;
        }

        protected abstract void SetLocalElementValue(TValue value);

        public override void HandleElementValueChange(INetworkConnection connection, object data)
        {
            try
            {
                // Try to get value as intended type and update item to use latest value
                var value = (TValue)Convert.ChangeType(data, typeof(TValue));
                SetLocalElementValue(value);

                // Invoke listeners
                _onValueChanged?.Invoke(value, connection);
                ValueChanged?.Invoke(value, connection);
            }
            catch (InvalidCastException)
            {
                Debug.LogWarning($"Invoking change on '{GetType()}', but cast was invalid. Expected '{typeof(TValue)}' but got '{data?.GetType()}'.");
                _onValueChanged?.Invoke(default(TValue), connection);
                ValueChanged?.Invoke(default(TValue), connection);
            }
        }
    }
}