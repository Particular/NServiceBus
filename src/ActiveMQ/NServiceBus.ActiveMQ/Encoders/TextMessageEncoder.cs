namespace NServiceBus.Transports.ActiveMQ.Encoders
{
    using System.Text;
    using Apache.NMS;

    public class TextMessageEncoder : IActiveMqMessageEncoder
    {
        public IMessage Encode(TransportMessage message, ISession session)
        {
            var contentType = message.Headers[Headers.ContentType];

            if (contentType == ContentTypes.Json || contentType == ContentTypes.Xml)
            {
                IMessage encoded = session.CreateTextMessage();

                if (message.Body != null)
                {
                    var messageBody = Encoding.UTF8.GetString(message.Body);
                    encoded = session.CreateTextMessage(messageBody);
                }

                return encoded;
            }

            return null;
        }
    }
}