namespace NServiceBus.Host
{
    /// <summary>
    /// Specify custom initialization for the endpoint.
    /// </summary>
    public interface IWantCustomInitialization
    {
        /// <summary>
        /// Perform custom actions to perform during initialization.
        /// </summary>
        void Init(Configure configure);
    }
}
