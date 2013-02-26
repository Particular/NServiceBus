namespace NServiceBus.Transports.ActiveMQ.Decoders
{
    using Apache.NMS;

    public class ByteMessageDecoder : IActiveMqMessageDecoder
    {
        public bool Decode(TransportMessage transportMessage, IMessage message)
        {
            var decoded = message as IBytesMessage;

            if (decoded != null)
            {
                // currently there is an issue in active mq NMS, accessing the content property 
                // multiple times will return different results
                byte[] content = decoded.Content;
                if (content != null)
                {
                    transportMessage.Body = content;
                }

                return true;
            }

            return false;
        }
    }
}