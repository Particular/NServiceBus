namespace NServiceBus.Transports
{
    using System;
    using NServiceBus.ObjectBuilder;
    using Support;
    using Unicast;

    class AuditerWrapper : IAuditMessages
    {
        readonly IBuilder builder;

        public Type AuditerImplType { get; set; }

        public AuditerWrapper(IBuilder builder)
        {
            this.builder = builder;
        }

        public void Audit(SendOptions sendOptions, TransportMessage message)
        {
            ((dynamic)builder.Build(AuditerImplType)).Audit(sendOptions, message);
        }
    }

    class DefaultMessageAuditer
    {
        public ISendMessages MessageSender { get; set; }

        public Configure Configure { get; set; }

        public string EndpointName { get; set; }

        public void Audit(SendOptions sendOptions, OutgoingMessage message)
        {
            // Revert the original body if needed (if any mutators were applied, forward the original body as received)
            //todo: this will be solved when we switch to a stream
            //transportMessage.RevertToOriginalBodyIfNeeded();


            message.Headers[Headers.ProcessingMachine] = RuntimeEnvironment.MachineName;
            message.Headers[Headers.ProcessingEndpoint] = EndpointName;

            // Send the newly created transport message to the queue
            MessageSender.Send(message, new SendOptions(sendOptions.Destination)
            {
                TimeToBeReceived = sendOptions.TimeToBeReceived
            });
        }

        class Initialization : INeedInitialization
        {
            public void Customize(BusConfiguration configuration)
            {
                configuration.RegisterComponents(c => c.ConfigureComponent<DefaultMessageAuditer>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(t => t.EndpointName, configuration.Settings.EndpointName()));

                configuration.RegisterComponents(c => c.ConfigureComponent<AuditerWrapper>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(t => t.AuditerImplType, typeof(DefaultMessageAuditer)));
            }
        }
    }
}