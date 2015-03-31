namespace NServiceBus.Transports
{
    class DefaultMessageAuditer
    {
        public ISendMessages MessageSender { get; set; }

        public void Audit(TransportSendOptions sendOptions, OutgoingMessage message)
        {
            MessageSender.Send(message, new TransportSendOptions(sendOptions.Destination));
        }

        class Initialization : INeedInitialization
        {
            public void Customize(BusConfiguration configuration)
            {
                configuration.RegisterComponents(c => c.ConfigureComponent<DefaultMessageAuditer>(DependencyLifecycle.InstancePerCall));

                configuration.RegisterComponents(c => c.ConfigureComponent<AuditerWrapper>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(t => t.AuditerImplType, typeof(DefaultMessageAuditer)));
            }
        }
    }
}