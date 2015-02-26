namespace NServiceBus.Faults.Forwarder.Config
{
    using Unicast.Queuing;

    class FaultsQueueCreator : IWantQueueCreated
    {
        public string ErrorQueue { get; set; }

        public string Address
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
