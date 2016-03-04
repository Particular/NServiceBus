namespace NServiceBus
{
    using Settings;
    using Transports;

    /// <summary>
    /// Transport definition for MSMQ.
    /// </summary>
    public class MsmqTransport : TransportDefinition
    {
        /// <summary>
        /// <see cref="TransportDefinition.ExampleConnectionStringForErrorMessage" />.
        /// </summary>
        public override string ExampleConnectionStringForErrorMessage => "cacheSendConnection=true;journal=false;deadLetter=true";

        /// <summary>
        /// <see cref="TransportDefinition.RequiresConnectionString" />.
        /// </summary>
        public override bool RequiresConnectionString => false;

        /// <summary>
        /// Initializes the transport infrastructure for msmq.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>the transport infrastructure for msmq.</returns>
        protected internal override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new MsmqTransportInfrastructure(settings, connectionString);
        }
    }
}