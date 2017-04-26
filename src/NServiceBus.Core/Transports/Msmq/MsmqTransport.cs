namespace NServiceBus
{
    using System;
    using Features;
    using Routing;
    using Settings;
    using Transport;

    /// <summary>
    /// Transport definition for MSMQ.
    /// </summary>
    public class MsmqTransport : TransportDefinition, IMessageDrivenSubscriptionTransport
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
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            string errorQueue;

            if (!settings.TryGetExplicitErrorQueueAddress(out errorQueue))
            {
                throw new Exception(
                    @"Faults forwarding requires an error queue to be specified.
            Take one of the following actions:
            - set the error queue at configuration time using 'EndpointConfiguration.SendFailedMessagesTo()'
            - add a 'MessageForwardingInCaseOfFaultConfig' section to the app.config
            - configure a global error queue in the registry using the powershell command: Set-NServiceBusLocalMachineSettings -ErrorQueue {address of error queue}");
            }

            settings.EnableFeature(typeof(InstanceMappingFileFeature));

            var msmqSettings = connectionString != null ? new MsmqConnectionStringBuilder(connectionString)
                .RetrieveSettings() : new MsmqSettings();

            msmqSettings.UseDeadLetterQueueForMessagesWithTimeToBeReceived = settings.GetOrDefault<bool>(UseDeadLetterQueueForMessagesWithTimeToBeReceived);

            settings.Set<MsmqSettings>(msmqSettings);

            return new MsmqTransportInfrastructure(settings);
        }

        internal const string UseDeadLetterQueueForMessagesWithTimeToBeReceived = "UseDeadLetterQueueForMessagesWithTimeToBeReceived";
    }
}