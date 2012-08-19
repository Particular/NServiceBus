namespace NServiceBus.Unicast.Queuing.Installers
{
    using Unicast.Queuing;

    public class EndpointInputQueueCreator : IWantQueueCreated
    {
        public static bool Enabled { get; set; }

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
            get { return !Enabled; }
        }
    }
}
