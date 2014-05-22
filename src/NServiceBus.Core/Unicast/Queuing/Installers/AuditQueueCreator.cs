namespace NServiceBus.Unicast.Queuing.Installers
{
    using Features;

    class AuditQueueCreator : IWantQueueCreated
    {
        public Address AuditQueue { get; set; }

        /// <summary>
        /// Address of queue the implementer requires.
        /// </summary>
        public Address Address
        {
            get { return AuditQueue; }
        }

        public bool ShouldCreateQueue(Configure config)
        {
            return config.Features.IsEnabled<Audit>();
        }
    }
}
