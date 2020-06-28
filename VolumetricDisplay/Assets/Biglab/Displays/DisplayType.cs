namespace Biglab.Displays
{
    public enum DisplayType
    {
        /// <summary>
        /// A virtual display using calibration data to produce the mesh.
        /// </summary>
        VirtualByCalibration,

        /// <summary>
        /// A phyisical display for rendering to a mosaic projector based display.
        /// </summary>
        Mosaic,

        /// <summary>
        /// A completely virtual display for rendering to the scene using <see cref="BiglabProjector"/> or <see cref="PerspectiveProjector"/> components.
        /// </summary>
        Virtual
    }
}