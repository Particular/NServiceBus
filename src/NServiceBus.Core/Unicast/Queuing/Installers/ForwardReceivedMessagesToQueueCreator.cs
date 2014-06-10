namespace NServiceBus.Unicast.Queuing.Installers
{

    class ForwardReceivedMessagesToQueueCreator : IWantQueueCreated
    {
        public Address Address{get; private set;}
        public bool Enabled { get; set; }

        public bool ShouldCreateQueue(Configure config)
        {
            return Enabled;
        }
    }
}