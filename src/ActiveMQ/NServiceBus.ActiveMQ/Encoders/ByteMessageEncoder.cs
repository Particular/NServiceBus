﻿namespace NServiceBus.Transport.ActiveMQ.Encoders
{
    using Apache.NMS;

    public class ByteMessageEncoder : IActiveMqMessageEncoder
    {
        public IMessage Encode(TransportMessage message, ISession session)
        {
            string contentType = message.Headers[Headers.ContentType];

            if (contentType == ContentTypes.Bson || contentType == ContentTypes.Binary)
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