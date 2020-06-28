namespace Biglab.Calibrations
{
    /// <summary>
    /// What mode the model is currently set to.
    /// GroundTruth - The parameters in this mode represent are exactly correct
    /// ErrorParameter - The parameters in this mode represent additive error to the GroundTruth
    /// Approximation - The parameters in this mode represent an approximation of the GroundTruth
    /// </summary>
    public enum ModelMode
    {
        GroundTruth,
        ErrorParameter,
        Approximation
    }
}