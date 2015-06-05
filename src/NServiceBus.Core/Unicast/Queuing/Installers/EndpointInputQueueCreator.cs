namespace NServiceBus.Unicast.Queuing.Installers
{
    class EndpointInputQueueCreator : IWantQueueCreated
    {
        public EndpointInputQueueCreator(Configure config)
        {
            Address = config.LocalAddress;
        }

        /// <summary>
        /// Endpoint input name
        /// </summary>
        public string Address { get; private set; }

        public bool ShouldCreateQueue()
        {
            return true;
        }
    }
}
