namespace NServiceBus.Host
{
    /// <summary>
    /// Implementers will be invoked when the endpoint starts up.
    /// </summary>
    public interface IWantToRunAtStartup
    {
        /// <summary>
        /// Method called at startup.
        /// </summary>
        void Run();

        /// <summary>
        /// Method called on shutdown.
        /// </summary>
        void Stop();
    }
}
