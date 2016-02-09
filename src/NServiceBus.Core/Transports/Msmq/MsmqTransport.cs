namespace NServiceBus
{
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    /// Transport definition for MSMQ.
    /// </summary>
    public class MsmqTransport : TransportDefinition
    {
        /// <summary>
        /// Initializes the transport infrastructure for msmq.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>the transport infrastructure for msmq.</returns>
        protected internal override TransportInfrastructure Initialize(SettingsHolder settings)
        {
            return new MsmqTransportInfrastructure(settings);
        }
    }
}