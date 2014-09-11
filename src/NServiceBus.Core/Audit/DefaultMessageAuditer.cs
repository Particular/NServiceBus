namespace NServiceBus.Transports
{
    using Support;
    using Unicast;

    class DefaultMessageAuditer : IAuditMessages
    {
        public ISendMessages MessageSender { get; set; }

        public Configure Configure { get; set; }

        public string EndpointName { get; set; }

        public void Audit(SendOptions sendOptions, TransportMessage transportMessage)
        {
            // Revert the original body if needed (if any mutators were applied, forward the original body as received)
            transportMessage.RevertToOriginalBodyIfNeeded();

            // Create a new transport message which will contain the appropriate headers
            var messageToForward = new TransportMessage(transportMessage.Id, transportMessage.Headers)
            {
                Body = transportMessage.Body,
                Recoverable = transportMessage.Recoverable,
                TimeToBeReceived = sendOptions.TimeToBeReceived.HasValue ? sendOptions.TimeToBeReceived.Value : transportMessage.TimeToBeReceived
            };

            messageToForward.Headers[Headers.ProcessingMachine] = RuntimeEnvironment.MachineName;
            messageToForward.Headers[Headers.ProcessingEndpoint] = EndpointName;

            if (transportMessage.ReplyToAddress != null)
            {
                messageToForward.Headers[Headers.OriginatingAddress] = transportMessage.ReplyToAddress.ToString();
            }

            // Send the newly created transport message to the queue
            MessageSender.Send(messageToForward, new SendOptions(sendOptions.Destination)
            {
                ReplyToAddress = Configure.PublicReturnAddress
            });
        }

        class Initialization : INeedInitialization
        {
            public void Customize(BusConfiguration configuration)
            {
                configuration.RegisterComponents(c => c.ConfigureComponent<DefaultMessageAuditer>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(t => t.EndpointName, configuration.Settings.EndpointName()));
            }
        }
    }
}