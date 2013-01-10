namespace NServiceBus.Transport.ActiveMQ.Decoders
{
    using Apache.NMS;

    public class ByteMessageDecoder : IActiveMqMessageDecoder
    {
        public bool Decode(TransportMessage transportMessage, IMessage message)
        {
            var decoded = message as IBytesMessage;

            if (decoded != null)
            {
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