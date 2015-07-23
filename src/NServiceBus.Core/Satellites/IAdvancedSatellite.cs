namespace NServiceBus
{
    /// <summary>
    /// Interface for satellites that needs more control over how the receiver is being setup.
    /// </summary>
    [ObsoleteEx(
        Message = "ISatellite is no longer an extension point. In order to create a satellite one must create a feature that uses AddSatellitePipeline() method and a class that inherits from SatelliteBehavior that is used for processing the messages.",
        RemoveInVersion = "7",
        TreatAsErrorFromVersion = "6")]
    public interface IAdvancedSatellite
    {
        
    }
}