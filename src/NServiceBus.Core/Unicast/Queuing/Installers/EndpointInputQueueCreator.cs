namespace NServiceBus.Unicast.Queuing.Installers
{
    using NServiceBus.Settings;

    class EndpointInputQueueCreator : IWantQueueCreated
    {
        public EndpointInputQueueCreator(ReadOnlySettings settings)
        {
            Address = settings.LocalAddress();
        }

        public string Address { get; private set; }

        public bool ShouldCreateQueue()
        {
            return true;
        }
    }
}
