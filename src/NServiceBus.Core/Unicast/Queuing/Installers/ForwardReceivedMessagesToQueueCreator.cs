namespace NServiceBus.Unicast.Queuing.Installers
{

    class ForwardReceivedMessagesToQueueCreator : IWantQueueCreated
    {
        public Address Address{get; set; }
        public bool Enabled { get; set; }

        public bool ShouldCreateQueue()
        {
            return Enabled;
        }
    }
}