namespace NServiceBus.TransportTests
{
    using System.Threading.Tasks;
    using Settings;

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
        /// <param name="transactionMode">Transaction mode for which transport seam should be configured.</param>
        /// <returns>Transport configuration result <see cref="TransportConfigurationResult"/></returns>
        TransportConfigurationResult Configure(SettingsHolder settings, TransportTransactionMode transactionMode);

        /// <summary>
        /// Gives the transport chance to clean up after the test is complete. Implementations of this class may store
        /// private variables during Configure to use during the cleanup phase.
        /// </summary>
        /// <returns>An async Task.</returns>
        Task Cleanup();
    }
}