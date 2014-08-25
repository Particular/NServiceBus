namespace NServiceBus.Unicast.Queuing.Installers
{
    class AuditQueueCreator : IWantQueueCreated
    {
        public string AuditQueue { get; set; }

        public string Address
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
