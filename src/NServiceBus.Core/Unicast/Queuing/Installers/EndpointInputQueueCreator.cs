namespace NServiceBus.Unicast.Queuing.Installers
{
    class EndpointInputQueueCreator : IWantQueueCreated
    {
        /// <summary>
        /// Endpoint input name
        /// </summary>
        public Address Address
        {
            get { return Address.Local; }
        }

        public bool ShouldCreateQueue(Configure config)
        {
            return true;
        }
    }
}
