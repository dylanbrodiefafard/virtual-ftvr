using System;

namespace Biglab.Remote
{
    [Serializable]
    public class RemoteMenuValueChangeMessage
    {
        /// <summary>
        /// The unique identifier of the element.
        /// </summary>
        public int Id;

        /// <summary>
        /// The elements value.
        /// </summary>
        public object Value;

        public RemoteMenuValueChangeMessage(int id, object value)
        {
            Id = id;
            Value = value;
        }
    }
}