namespace Biglab.IO.Networking
{
    public enum MessageType : byte
    {
        /// <summary>
        /// Message sent from the server to the clients informing them of the server state ( which scene, etc )
        /// </summary>
        ServerState,

        /// <summary>
        /// Message sent from the client to the server to give a health report on the image stream.
        /// </summary>
        StreamState,

        /// <summary>
        /// Message sent from the server to the clients with image data.
        /// </summary>
        Image,

        /// <summary>
        /// Message sent to the client to add a menu button.
        /// </summary>
        InterfaceAddition,

        /// <summary>
        /// Message sent to the client to remove a menu button.
        /// </summary>
        InterfaceRemoval,

        /// <summary>
        /// Message sent from the client to the server to inform about a value/input interaction.
        /// </summary>
        ValueChanged,

        /// <summary>
        /// Message sent from the client to the server to inform about touch the device screen.
        /// </summary>
        TouchEvent
    }
}