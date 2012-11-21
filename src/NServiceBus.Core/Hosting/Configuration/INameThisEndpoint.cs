namespace NServiceBus
{
    /// <summary>
    /// Implement this interface in the endpoint configuration class if you want to give this endpoint your own name
    /// </summary>
    public interface INameThisEndpoint
    {
        /// <summary>
        /// Returns the name to be used for this endpoint
        /// </summary>
        /// <returns></returns>
        string GetName();
    }
}