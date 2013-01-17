namespace NServiceBus.Hosting
{
    using Installation;

    /// <summary>
    /// Identifies a host
    /// </summary>
    public interface IHost
    {
        /// <summary>
        /// Does startup work.
        /// </summary>
        void Start();

        /// <summary>
        /// Does shutdown work.
        /// </summary>
        void Stop();

        /// <summary>
        /// Performs necessary installation
        /// </summary>
        void Install<TEnvironment>(string username) where TEnvironment : IEnvironment;
    }
}