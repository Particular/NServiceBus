using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast.Transport.Http
{
    public class SendStateObject
    {
        private readonly TransportMessage message;
        private readonly string destination;

        public SendStateObject(TransportMessage message, string destination)
        {
            this.message = message;
            this.destination = destination;
        }

        public TransportMessage Message
        {
            get { return this.message; }
        }

        public string Destination
        {
            get { return this.destination; }
        }
    }
}
