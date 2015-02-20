namespace NServiceBus.Faults.Forwarder.Config
{
    using Unicast.Queuing;

    class FaultsQueueCreator : IWantQueueCreated
    {
        public Address ErrorQueue { get; set; }

        public Address Address
        {
            get { return ErrorQueue; }
        }

        public bool Enabled { get; set; }

        public bool ShouldCreateQueue()
        {
            return Enabled;
        }
    }
}
