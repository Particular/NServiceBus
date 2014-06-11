namespace NServiceBus.Unicast.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.XML;

    class Helpers
    {
        public static TransportMessage EmptyTransportMessage(Address replyToAddress = null)
        {
            if (replyToAddress == null)
            {
                replyToAddress = Address.Parse("myClient");
            }
            return new TransportMessage(Guid.NewGuid().ToString(),new Dictionary<string, string>(),replyToAddress);
        }

        public static TransportMessage EmptySubscriptionMessage()
        {
            var subscriptionMessage = new TransportMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), Address.Parse("mySubscriber")) { MessageIntent = MessageIntentEnum.Subscribe };

            subscriptionMessage.Headers[Headers.SubscriptionMessageType] =
                "NServiceBus.Unicast.Tests.MyMessage, Version=3.0.0.0, Culture=neutral, PublicKeyToken=9fc386479f8a226c";

            return subscriptionMessage;
        }

        public static TransportMessage MessageThatFailsToSerialize()
        {
            var m = EmptyTransportMessage();
            m.Body = new byte[1];
            return m;
        }

        public static TransportMessage Serialize<T>(T message,bool nullReplyToAddress = false)
        {
            var s = new XmlMessageSerializer(new MessageMapper())
            {
                Conventions = new Conventions()
            };

            s.Initialize(new[] { typeof(T) });

            var m = EmptyTransportMessage();

            if (nullReplyToAddress)
            {
                m = new TransportMessage();
            }

            using (var stream = new MemoryStream())
            {
                s.Serialize(message, stream);
                m.Body = stream.ToArray();
            }

            m.Headers[Headers.EnclosedMessageTypes] = typeof(T).FullName;


            return m;
        }
    }
}