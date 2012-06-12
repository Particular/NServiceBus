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
        /// Serializes the given messages to the given stream.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="stream"></param>
        public void Serialize(object[] messages, Stream stream)
        {
            binaryFormatter.Serialize(stream, new List<object>(messages));
        }

        /// <summary>
        /// Deserializes the given stream returning an array of messages.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public object[] Deserialize(Stream stream)
        {
            if (stream == null)
                return null;

            var body = binaryFormatter.Deserialize(stream) as List<object>;

            if (body == null)
                return null;

            var result = new object[body.Count];

            int i = 0;
            foreach (object m in body)
                result[i++] = m;

            return result;
        }

        private readonly BinaryFormatter binaryFormatter = new BinaryFormatter();
    }
}
