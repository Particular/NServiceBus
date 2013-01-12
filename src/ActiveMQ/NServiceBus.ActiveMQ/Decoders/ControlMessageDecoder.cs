namespace NServiceBus.Transport.ActiveMQ.Decoders
{
    using Apache.NMS;

    public class ControlMessageDecoder : IActiveMqMessageDecoder
    {
        public bool Decode(TransportMessage transportMessage, IMessage message)
        {
            if(message.IsControlMessage())
            {
                var decoded = (IBytesMessage)message;

                if (decoded.Content != null)
                {
                    transportMessage.Body = decoded.Content;
                }

                return true;
            }

            return false;
        }
    }
}