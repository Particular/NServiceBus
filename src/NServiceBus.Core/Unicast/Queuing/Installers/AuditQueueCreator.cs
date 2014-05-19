namespace NServiceBus.Unicast.Queuing.Installers
{
    using Audit;
    using Features;

    class AuditQueueCreator : IWantQueueCreated
    {
        public MessageAuditer Auditer { get; set; }

        /// <summary>
        /// Address of queue the implementer requires.
        /// </summary>
        public Address Address
        {
            get { return Auditer.AuditQueue; }
        }

        public bool ShouldCreateQueue(Configure config)
        {
            return config.Features.IsEnabled<Audit>();
        }
    }
}
