using System;

namespace Biglab.Remote
{
    [Serializable]
    public class RemoteMenuStateMessage
    {
        /// <summary>
        /// The connection id according to the server.
        /// </summary>
        public int Id;

        public int ConnectionCount;

        public string SceneName;

        public string Description;

        public RemoteMenuStateMessage(int id, int count, string name, string description)
        {
            Id = id;
            ConnectionCount = count;
            Description = description;
            SceneName = name;
        }
    }
}
