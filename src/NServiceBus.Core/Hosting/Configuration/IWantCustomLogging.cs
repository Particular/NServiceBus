namespace NServiceBus
{
    /// <summary>
    /// If you want to specify your own logging,
    /// implement this interface on the class which implements <see cref="IConfigureThisEndpoint"/>. 
    /// </summary>
    public interface IWantCustomLogging
    {
        /// <summary>
        /// Initialize logging.
        /// </summary>
        void Init();
    }
}