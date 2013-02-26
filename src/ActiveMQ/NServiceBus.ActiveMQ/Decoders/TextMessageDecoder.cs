namespace NServiceBus.Transports.ActiveMQ.Decoders
{
    using System.Text;
    using Apache.NMS;

    public class TextMessageDecoder : IActiveMqMessageDecoder
    {
        public bool Decode(TransportMessage transportMessage, IMessage message)
        {
            var decoded = message as ITextMessage;

            if (decoded != null)
            {
                if (decoded.Text != null)
                {
                    transportMessage.Body = Encoding.UTF8.GetBytes(decoded.Text);
                }

                return true;
            }

            return false;
        }
    }
}