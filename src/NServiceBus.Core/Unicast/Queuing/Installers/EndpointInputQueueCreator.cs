namespace NServiceBus.Unicast.Queuing.Installers
{
    class EndpointInputQueueCreator : IWantQueueCreated
    {
        Address address;

        public EndpointInputQueueCreator(Configure config)
        {
            address = config.LocalAddress;
        }

        /// <summary>
        /// Endpoint input name
        /// </summary>
        public Address Address
        {
            get { return address; }
        }

        public bool ShouldCreateQueue()
        {
            return true;
        }
    }
}
