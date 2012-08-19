namespace NServiceBus.Unicast.Queuing.Installers
{
    using NServiceBus.Config;
    using Unicast.Queuing;
    using Logging;
    
    /// <summary>
    /// Signals to create forward received messages queue.
    /// </summary>
    public class ForwardReceivedMessagesToQueueCreator : IWantQueueCreated
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ForwardReceivedMessagesToQueueCreator)); 
        private readonly Address address = null;
        private static bool disable = true;

        public ForwardReceivedMessagesToQueueCreator()
        {
            disable = true;
            if (!EndpointInputQueueCreator.Enabled)
                return;

            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();

            if ((unicastConfig == null) || (string.IsNullOrEmpty(unicastConfig.ForwardReceivedMessagesTo)))
                return;

            address = Address.Parse(unicastConfig.ForwardReceivedMessagesTo);
            disable = false;
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
