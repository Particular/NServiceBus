using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NServiceBus.Serialization;

namespace NServiceBus.Serializers.Binary
{
    /// <summary>
    /// Binary implementation of the message serializer.
    /// </summary>
    public class MessageSerializer : IMessageSerializer
    {
        /// <summary>
        /// Doesn't do anything.
        /// </summary>
        /// <param name="types"></param>
        public void Initialize(params Type[] types)
        {
        }

        /// <summary>
        /// Serializes the given messages to the given stream.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="stream"></param>
        public void Serialize(IMessage[] messages, Stream stream)
        {
            binaryFormatter.Serialize(stream, new List<object>(messages));
        }

        /// <summary>
        /// Deserializes the given stream returning an array of messages.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public IMessage[] Deserialize(Stream stream)
        {
            var body = binaryFormatter.Deserialize(stream) as List<object>;

            if (body == null)
                return null;

            var result = new IMessage[body.Count];

            int i = 0;
            foreach (IMessage m in body)
                result[i++] = m;

            return result;
        }

        private readonly BinaryFormatter binaryFormatter = new BinaryFormatter();
    }
}
