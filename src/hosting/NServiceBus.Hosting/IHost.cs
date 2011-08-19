namespace NServiceBus.Hosting
{
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
    }
}