namespace NServiceBus
{
    /// <summary>
    /// Implement this interface in the endpoint configuration class if you want to give this endpoint your own name
    /// </summary>
    [ObsoleteEx(Replacement = "Configure.DefineEndpointName()", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]    
    public interface INameThisEndpoint
    {
        /// <summary>
        /// Returns the name to be used for this endpoint
        /// </summary>
        string GetName();
    }
}