namespace NServiceBus.Unicast.Queuing.Installers
{
    class EndpointInputQueueCreator : IWantQueueCreated
    {
        string address;

        public EndpointInputQueueCreator(Configure config)
        {
            address = config.LocalAddress;
        }

        /// <summary>
        /// Endpoint input name
        /// </summary>
        public string Address
        {
            get { return address; }
        }

        public bool ShouldCreateQueue()
        {
            return true;
        }
    }
}
