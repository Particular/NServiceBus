namespace NServiceBus.Unicast.Queuing.Installers
{
    public class EndpointInputQueueCreator : IWantQueueCreated
    {
        /// <summary>
        /// Endpoint input name
        /// </summary>
        public Address Address
        {
            get { return Address.Local; }
        }

        /// <summary>
        /// True if no need to create queue
        /// </summary>
        public bool IsDisabled
        {
            get { return false; }
        }
    }
}
