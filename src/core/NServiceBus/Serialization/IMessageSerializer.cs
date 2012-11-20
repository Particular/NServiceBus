namespace NServiceBus.Serialization
{
    using System.IO;
    using System.Collections.Generic;

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
        void Serialize(object[] messages, Stream stream);

        /// <summary>
        /// Deserializes from the given stream a set of messages.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="messageTypes">The list of message types to deserialize. If null the types must be infered from the serialized data</param>
        /// <returns></returns>
        object[] Deserialize(Stream stream,IEnumerable<string> messageTypes = null);
    }
}
