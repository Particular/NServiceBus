namespace NServiceBus.Transports.ActiveMQ.Decoders
{
    using Apache.NMS;

    public class ControlMessageDecoder : IActiveMqMessageDecoder
    {
        public bool Decode(TransportMessage transportMessage, IMessage message)
        {
            if (message.IsControlMessage())
            {
                var decoded = (IBytesMessage)message;

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