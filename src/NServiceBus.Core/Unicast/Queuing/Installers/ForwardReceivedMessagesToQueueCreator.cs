namespace NServiceBus.Unicast.Queuing.Installers
{

    class ForwardReceivedMessagesToQueueCreator : IWantQueueCreated
    {
        public string Address { get; private set; }
        public bool Enabled { get; set; }

        public bool ShouldCreateQueue()
        {
            return Enabled;
        }
    }
}