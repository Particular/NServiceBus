namespace NServiceBus.Unicast.Tests.Helpers
{
    using System.IO;
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.XML;

    class Helpers
    {
        public static TransportMessage EmptyTransportMessage()
        {
            return new TransportMessage();
        }

        public static TransportMessage EmptySubscriptionMessage()
        {
           var subscriptionMessage = new TransportMessage
            {
                MessageIntent = MessageIntentEnum.Subscribe,
                ReplyToAddress = Address.Parse("mySubscriber")
            };

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

        public static TransportMessage Serialize<T>(T message)
        {
            var s = new XmlMessageSerializer(new MessageMapper());
            s.Initialize(new[] { typeof(T) });

            var m = EmptyTransportMessage();

            using (var stream = new MemoryStream())
            {
                s.Serialize(new object[] { message }, stream);
                m.Body = stream.ToArray();
            }

            m.Headers[Headers.EnclosedMessageTypes] = typeof(T).FullName;


            return m;
        }
    }
}