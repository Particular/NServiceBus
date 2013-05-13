using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.ServiceBus.Messaging;

namespace NServiceBus.Unicast.Queuing.Azure.ServiceBus
{
    public class BrokeredMessageConverter
    {
        public TransportMessage ToTransportMessage(BrokeredMessage message)
        {
            TransportMessage t;
            var rawMessage = message.GetBody<byte[]>();

            if (message.Properties.Count == 0)
            {
                t = DeserializeMessage(rawMessage);
            }
            else
            {
                t = new TransportMessage
                        {
                            CorrelationId = message.CorrelationId,
                            TimeToBeReceived = message.TimeToLive
                        };

                foreach (var header in message.Properties)
                {
                    t.Headers[header.Key] = header.Value.ToString();
                }

                t.MessageIntent =
                    (MessageIntentEnum)
                    Enum.Parse(typeof(MessageIntentEnum), message.Properties[Headers.MessageIntent].ToString());
                t.Id = message.MessageId;
                if ( !String.IsNullOrWhiteSpace( message.ReplyTo ) )
                {
                    t.ReplyToAddress = Address.Parse( message.ReplyTo ); // Will this work?
                }

                t.Body = rawMessage;
            }

            if (t.Id == null) t.Id = Guid.NewGuid().ToString();

            return t;
        }

        private static TransportMessage DeserializeMessage(byte[] rawMessage)
        {
            var formatter = new BinaryFormatter();

            using (var stream = new MemoryStream(rawMessage))
            {
                var message = formatter.Deserialize(stream) as TransportMessage;

                if (message == null)
                    throw new SerializationException("Failed to deserialize message");

                return message;
            }
        }
    }
}