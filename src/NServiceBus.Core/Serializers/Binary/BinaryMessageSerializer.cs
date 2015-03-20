namespace NServiceBus.Serializers.Binary
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Xml.Linq;
    using Serialization;

    /// <summary>
    /// Binary implementation of the message serializer.
    /// </summary>
    public class BinaryMessageSerializer : IMessageSerializer
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BinaryMessageSerializer"/>.
        /// </summary>
        public BinaryMessageSerializer()
        {
            var surrogateSelector = new SurrogateSelector();
            surrogateSelector.AddSurrogate(typeof(XDocument), new StreamingContext(StreamingContextStates.All), new XContainerSurrogate());
            surrogateSelector.AddSurrogate(typeof(XElement), new StreamingContext(StreamingContextStates.All), new XElementSurrogate());

            binaryFormatter.SurrogateSelector = surrogateSelector;
        }
        
        /// <summary>
        /// Serializes the given set of messages into the given stream.
        /// </summary>
        /// <param name="message">Message to serialize.</param>
        /// <param name="stream">Stream for <paramref name="message"/> to be serialized into.</param>
        public void Serialize(object message, Stream stream)
        {
            Guard.AgainstNull(message, "message");
            Guard.AgainstNull(stream, "stream");
            binaryFormatter.Serialize(stream, new List<object> { message });
        }

        /// <summary>
        /// Deserializes from the given stream a set of messages.
        /// </summary>
        /// <param name="stream">Stream that contains messages.</param>
        /// <param name="messageTypes">The list of message types to deserialize. If null the types must be inferred from the serialized data.</param>
        /// <returns>Deserialized messages.</returns>
        public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
        {
            if (stream == null)
                return null;

            var body = binaryFormatter.Deserialize(stream) as List<object>;

            if (body == null)
                return null;

            var result = new object[body.Count];

            var i = 0;
            foreach (var m in body)
                result[i++] = m;

            return result;
        }

        /// <summary>
        /// Gets the content type into which this serializer serializes the content to 
        /// </summary>
        public string ContentType { get{ return ContentTypes.Binary;}}

        readonly BinaryFormatter binaryFormatter = new BinaryFormatter();
    }
}
