namespace NServiceBus.Transports.ActiveMQ.Encoders
{
    using Apache.NMS;
    using Unicast.Transport;

    public class ControlMessageEncoder : IActiveMqMessageEncoder
    {
        public IMessage Encode(TransportMessage message, ISession session)
        {
            if (message.IsControlMessage())
            {
                IMessage encoded = session.CreateBytesMessage();

                if (message.Body != null)
                {
                    encoded = session.CreateBytesMessage(message.Body);
                }

                return encoded;
            }

            return null;
        }
    }
}