namespace NServiceBus.Unicast.Queuing.Installers
{
    using Audit;
    using Features;
    using NServiceBus.Config;
    using Utils;

    /// <summary>
    /// Signals to create forward received messages queue.
    /// </summary>
    public class ForwardReceivedMessagesToQueueCreator : IWantQueueCreated
    {
        public MessageAuditer Auditer { get; set; }

        /// <summary>
        /// Address of queue the implementer requires.
        /// </summary>
        public Address Address
        {
            get { return Auditer.AuditQueue; }
        }

        /// <summary>
        /// True if no need to create queue
        /// </summary>
        public bool IsDisabled
        {
            get { return !Feature.IsEnabled<Audit>(); }
        }
    }
}
