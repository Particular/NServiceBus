using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NServiceBus.Serialization;

namespace NServiceBus.Serializers.Binary
{
    public class MessageSerializer : IMessageSerializer
    {
        public void Initialize(params Type[] types)
        {
            foreach (Type t in types)
                if (!t.IsSerializable)
                    throw new InvalidOperationException("Cannot register a non-serializable type: " +
                                                        t.FullName);
        }

        public void Serialize(IMessage[] messages, Stream stream)
        {
            this.binaryFormatter.Serialize(stream, new List<object>(messages));
        }

        public IMessage[] Deserialize(Stream stream)
        {
            List<object> body = this.binaryFormatter.Deserialize(stream) as List<object>;

            if (body == null)
                return null;

            IMessage[] result = new IMessage[body.Count];

            int i = 0;
            foreach (IMessage m in body)
                result[i++] = m;

            return result;
        }

        private readonly BinaryFormatter binaryFormatter = new BinaryFormatter();
    }
}
