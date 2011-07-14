using System;
using System.IO;

namespace NServiceBus.Serialization
{
    /// <summary>
    /// Interface used for serializing and deserializing messages.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes the given set of messages into the given stream.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="stream"></param>
        void Serialize(IMessage[] messages, Stream stream);

        /// <summary>
        /// Deserializes from the given stream a set of messages.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        IMessage[] Deserialize(Stream stream);
    }
}
