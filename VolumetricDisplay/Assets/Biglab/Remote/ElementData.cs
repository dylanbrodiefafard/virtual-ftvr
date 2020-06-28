using System;
using System.Collections.Generic;

using UnityEngine;

namespace Biglab.Remote.Client
{
    [Serializable]
    public abstract class ElementData
    {
        public abstract ElementType Type { get; }

        /// <summary>
        /// Unique ID to identify this element.
        /// </summary>
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// A title to group similar items.
        /// </summary>
        public string Group
        {
            get { return _group; }
            set { _group = value; }
        }

        /// <summary>
        /// A sorting number to organize items within their group.
        /// </summary>
        public int Order
        {
            get { return _groupOrder; }
            set { _groupOrder = value; }
        }

        [ReadOnly, SerializeField]
        private int _id;

        [Space]

        [SerializeField]
        private string _group;

        [SerializeField]
        private int _groupOrder;

        // YC: Further model the interface element by imposing a list of values on it.
        // This list represents the historical values of every interface element 
        private readonly Queue<object> _values = new Queue<object>();

        internal void AddValueToQueue(object value)
        {
            _values.Enqueue(value);
        }

        internal void ClearAllButLast()
        {
            while (_values.Count > 1)
            {
                _values.Dequeue();
            }
        }

        internal bool TryGetLatestCommand(out RemoteMenuValueChangeMessage message)
        {
            if (_values.Count > 0)
            {
                message = new RemoteMenuValueChangeMessage(Id, _values.Dequeue());
                return true;
            }

            message = null;
            return false;
        }
    }
}