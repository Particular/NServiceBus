using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using NServiceBus.Serialization;

namespace NServiceBus.Serializers.XML
{
    public class MessageSerializer : IMessageSerializer
    {
        public void Initialize(params Type[] types)
        {
            this.xmlSerializer = new XmlSerializer(typeof(object), types);
        }

        public void Serialize(IMessage[] messages, Stream stream)
        {
            this.xmlSerializer.Serialize(stream, new List<object>(messages));
        }

        public IMessage[] Deserialize(Stream stream)
        {
            List<object> body = this.xmlSerializer.Deserialize(stream) as List<object>;

            if (body == null)
                return null;

            IMessage[] result = new IMessage[body.Count];

            int i = 0;
            foreach (IMessage m in body)
                result[i++] = m;

            return result;
        }

        private XmlSerializer xmlSerializer = null;
    }
}
