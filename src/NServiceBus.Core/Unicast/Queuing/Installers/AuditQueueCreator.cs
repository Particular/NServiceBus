namespace NServiceBus.Unicast.Queuing.Installers
{
    class AuditQueueCreator : IWantQueueCreated
    {
        public Address AuditQueue { get; set; }

        public Address Address
        {
            get { return AuditQueue; }
        }

        public bool Enabled { get; set; }

        public bool ShouldCreateQueue()
        {
            return Enabled;
        }
    }
}
