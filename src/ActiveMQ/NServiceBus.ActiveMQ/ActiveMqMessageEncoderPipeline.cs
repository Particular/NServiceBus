namespace NServiceBus.Transport.ActiveMQ
{
    using System;
    using System.Collections.Generic;

    using Apache.NMS;

    using NServiceBus.Transport.ActiveMQ.Encoders;

    public class ActiveMqMessageEncoderPipeline : IActiveMqMessageEncoderPipeline
    {
        private readonly IEnumerable<IActiveMqMessageEncoder> encoders;

        public ActiveMqMessageEncoderPipeline()
            : this(new IActiveMqMessageEncoder[] { new ControlMessageEncoder(), new TextMessageEncoder(), new ByteMessageEncoder(), })
        {
        }

        public ActiveMqMessageEncoderPipeline(IEnumerable<IActiveMqMessageEncoder> encoders)
        {
            this.encoders = encoders;
        }

        public IMessage Encode(TransportMessage message, ISession session)
        {
            foreach (var encoder in this.encoders)
            {
                IMessage encoded = encoder.Encode(message, session);
                if (encoded != null)
                {
                    return encoded;
                }
            }

            throw new InvalidOperationException("Unable to encode provided message to ActiveMQ message.");
        }
    }
}