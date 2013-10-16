namespace NServiceBus.Unicast.Queuing.Installers
{
    using System.ComponentModel;
    using NServiceBus.Config;

    /// <summary>
    /// Signals to create forward received messages queue.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class ForwardReceivedMessagesToQueueCreator : IWantQueueCreated
    {

        public ForwardReceivedMessagesToQueueCreator()
        {
            IsDisabled = true;

            var unicastConfig = Configure.GetConfigSection<UnicastBusConfig>();

            if ((unicastConfig != null) && (!string.IsNullOrEmpty(unicastConfig.ForwardReceivedMessagesTo)))
            {
                Address = Address.Parse(unicastConfig.ForwardReceivedMessagesTo);
                IsDisabled = false;
            }
        }

        /// <summary>
        /// Address of queue the implementer requires.
        /// </summary>
        public Address Address{get; private set;}

        /// <summary>
        /// True if no need to create queue
        /// </summary>
        public bool IsDisabled{get;private set;}
    }
}