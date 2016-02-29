namespace NServiceBus
{
    using System;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using NServiceBus.Transports.Msmq;

    /// <summary>
    /// Transport definition for MSMQ.
    /// </summary>
    public class MsmqTransport : TransportDefinition
    {
        /// <summary>
        /// Initializes the transport infrastructure for msmq.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>the transport infrastructure for msmq.</returns>
        protected internal override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            SubscriptionStoreDefinition subscriptionStoreDefinition;
            if (!settings.TryGet(out subscriptionStoreDefinition))
            {
                throw new Exception("When using MSMQ transport you need to specify subscription store using UseSubscriptionStore<T>() method.");
            }
            return new MsmqTransportInfrastructure(settings, connectionString, subscriptionStoreDefinition.Initialize(settings));
        }

        /// <summary>
        /// <see cref="TransportDefinition.ExampleConnectionStringForErrorMessage"/>.
        /// </summary>
        public override string ExampleConnectionStringForErrorMessage => "cacheSendConnection=true;journal=false;deadLetter=true";

        /// <summary>
        /// <see cref="TransportDefinition.RequiresConnectionString"/>.
        /// </summary>
        public override bool RequiresConnectionString => false;
    }
}