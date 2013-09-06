namespace NServiceBus.Transports.ActiveMQ
{
    using System;
    using System.Collections.Generic;
    using Apache.NMS;
    using Decoders;

    public class ActiveMqMessageDecoderPipeline : IActiveMqMessageDecoderPipeline
    {
        private readonly IEnumerable<IActiveMqMessageDecoder> decoders;

        public ActiveMqMessageDecoderPipeline()
            : this(new IActiveMqMessageDecoder[] { new ControlMessageDecoder(), new TextMessageDecoder(), new ByteMessageDecoder(), })
        {
        }

        public ActiveMqMessageDecoderPipeline(IEnumerable<IActiveMqMessageDecoder> decoders)
        {
            this.decoders = decoders;
        }

        public void Decode(TransportMessage transportMessage, IMessage message)
        {
            foreach (var decoder in decoders)
            {
                bool decoded = decoder.Decode(transportMessage, message);
                if (decoded)
                {
                    return;
                }
            }

            throw new InvalidOperationException("Unable to decode provided message body from ActiveMQ message.");
        }
    }
}