namespace NServiceBus.Unicast.Queuing.Installers
{
    using NServiceBus.Config;
    using Utils;

    /// <summary>
    /// Signals to create forward received messages queue.
    /// </summary>
    public class ForwardReceivedMessagesToQueueCreator : IWantQueueCreated
    {
        private readonly Address address;
        private readonly bool disable = true;

        public ForwardReceivedMessagesToQueueCreator()
        {
            disable = true;

            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();

            if ((unicastConfig != null) && (!string.IsNullOrEmpty(unicastConfig.ForwardReceivedMessagesTo)))
            {
                address = Address.Parse(unicastConfig.ForwardReceivedMessagesTo);
                disable = false;
                return;
            }

            var forwardQueue = RegistryReader<string>.Read("AuditQueue");
            if (!string.IsNullOrWhiteSpace(forwardQueue))
            {
                address = Address.Parse(forwardQueue);
                disable = false;
            }
        }

        /// <summary>
        /// Address of queue the implementer requires.
        /// </summary>
        public Address Address
        {
            get { return address; }
        }

        /// <summary>
        /// True if no need to create queue
        /// </summary>
        public bool IsDisabled
        {
            get { return disable; }
        }
    }
}
