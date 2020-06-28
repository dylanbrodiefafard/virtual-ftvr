using System;
using System.Collections.Generic;

namespace Biglab.Remote
{
    public static class RemoteInput
    {
        /// <summary>
        /// An event invoked when remote touches are updated.
        /// </summary>
        public static event Action<int, RemoteTouch[]> Touched;

        private static readonly Dictionary<int, RemoteTouch[]> _touches = new Dictionary<int, RemoteTouch[]>();

        internal static void NotifyTouches(int id, RemoteTouch[] touches)
        {
            // Store latest touch data for connection id
            _touches[id] = touches;

            // 
            Touched?.Invoke(id, touches);
        }

        /// <summary>
        /// Gets the latest number of touches for the given connection id.
        /// </summary>
        public static int GetTouchCount(int id)
        {
            if (_touches.ContainsKey(id))
            {
                return _touches[id].Length;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets the latest set of touches for the given connection id.
        /// </summary>
        public static RemoteTouch[] GetTouches(int id)
        {
            if (_touches.ContainsKey(id))
            {
                return _touches[id];
            }
            else
            {
                return Array.Empty<RemoteTouch>();
            }
        }
    }
}