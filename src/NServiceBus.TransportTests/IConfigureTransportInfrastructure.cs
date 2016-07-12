namespace NServiceBus.TransportTests
{
    using System.Threading.Tasks;
    using Settings;
    using Transport;

    /// <summary>
    /// Provide a mechanism in components tests for transports
    /// to configure a transport infrastructure for a test and then clean up afterwards.
    /// </summary>
    public interface IConfigureTransportInfrastructure
    {
        /// <summary>
        /// Gives the transport a chance to configure before the test starts.
        /// </summary>
        /// <param name="settings">The settings to be passed into the infrastructure.</param>
        /// <returns>The created transport infrastructure.</returns>
        TransportInfrastructure Configure(SettingsHolder settings);

        /// <summary>
        /// Gives the transport chance to clean up after the test is complete. Implementations of this class may store
        /// private variables during Configure to use during the cleanup phase.
        /// </summary>
        /// <returns>An async Task.</returns>
        Task Cleanup();
    }
}