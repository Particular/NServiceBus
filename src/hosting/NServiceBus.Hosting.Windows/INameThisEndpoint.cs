namespace NServiceBus
{
    /// <summary>
    /// Implement this interface in the endpoint configuration class if you want to give this endpoint your own name
    /// </summary>
    [ObsoleteEx(Replacement = "Configure.DefineEndpointName()", TreatAsErrorFromVersion = "3.4", RemoveInVersion = "4.0")]    
    public interface INameThisEndpoint
    {
        /// <summary>
        /// Returns the name to be used for this endpoint
        /// </summary>
        /// <returns></returns>
        string GetName();
    }
}