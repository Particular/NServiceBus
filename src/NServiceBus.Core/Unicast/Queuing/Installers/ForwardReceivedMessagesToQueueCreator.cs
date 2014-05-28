namespace NServiceBus.Unicast.Queuing.Installers
{
    using NServiceBus.Config;

    class ForwardReceivedMessagesToQueueCreator : IWantQueueCreated
    {
        public Address Address{get; private set;}

        public bool ShouldCreateQueue(Configure config)
        {
            var unicastConfig = config.Settings.GetConfigSection<UnicastBusConfig>();

            if ((unicastConfig != null) && (!string.IsNullOrEmpty(unicastConfig.ForwardReceivedMessagesTo)))
            {
                Address = Address.Parse(unicastConfig.ForwardReceivedMessagesTo);
                return true;
            }

            return false;
        }
    }
}