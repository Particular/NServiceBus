namespace NServiceBus.Unicast.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using MessageInterfaces.MessageMapper.Reflection;
    using Serializers.XML;
    using Transport;

    class Helpers
    {
        public static TransportMessage EmptyTransportMessage()
        {
            var id = Guid.NewGuid().ToString();

            return new TransportMessage
                       {
                           Headers = new Dictionary<string, string>(),
                           Id = id,
                           IdForCorrelation = id
                       };
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
            s.Initialize(new []{typeof(T)});

            var m = EmptyTransportMessage();

            using(var stream = new MemoryStream())
            {
                s.Serialize(new object[]{message},stream);
                m.Body = stream.ToArray();
            }

            return m;
        }
    }
}