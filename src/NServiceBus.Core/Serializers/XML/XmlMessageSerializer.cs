namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using MessageInterfaces;
    using Serialization;

    class XmlMessageSerializer : IMessageSerializer
    {
        /// <summary>
        /// Initializes an instance of a <see cref="XmlMessageSerializer" />.
        /// </summary>
        /// <param name="mapper">Message Mapper.</param>
        /// <param name="conventions">The endpoint conventions.</param>
        public XmlMessageSerializer(IMessageMapper mapper, Conventions conventions)
        {
            Guard.AgainstNull(nameof(mapper), mapper);
            Guard.AgainstNull(nameof(conventions), conventions);
            this.mapper = mapper;
            this.conventions = conventions;
        }

        /// <summary>
        /// The namespace to place in outgoing XML.
        /// <para>If the provided namespace ends with trailing forward slashes, those will be removed on the fly.</para>
        /// </summary>
        public string Namespace
        {
            get { return nameSpace; }
            set { nameSpace = TrimPotentialTrailingForwardSlashes(value); }
        }

        /// <summary>
        /// If true, then the serializer will use a sanitizing stream to skip invalid characters from the stream before parsing.
        /// </summary>
        public bool SanitizeInput { get; set; }

        /// <summary>
        /// Removes the wrapping of properties containing XDocument or XElement with property name as root element.
        /// </summary>
        public bool SkipWrappingRawXml { get; set; }

        /// <summary>
        /// Deserializes from the given stream a set of messages.
        /// </summary>
        /// <param name="stream">Stream that contains messages.</param>
        /// <param name="messageTypesToDeserialize">
        /// The list of message types to deserialize. If null the types must be inferred
        /// from the serialized data.
        /// </param>
        /// <returns>Deserialized messages.</returns>
        public object[] Deserialize(Stream stream, IList<Type> messageTypesToDeserialize = null)
        {
            if (stream == null)
            {
                return null;
            }

            var deserializer = new XmlDeserialization(mapper, cache, SkipWrappingRawXml, SanitizeInput);
            return deserializer.Deserialize(stream, messageTypesToDeserialize);
        }

        /// <summary>
        /// Gets the content type into which this serializer serializes the content to.
        /// </summary>
        public string ContentType => ContentTypes.Xml;

        /// <summary>
        /// Serializes the given messages to the given stream.
        /// </summary>
        public void Serialize(object message, Stream stream)
        {
            var messageType = mapper.GetMappedTypeFor(message.GetType());
            using (var serializer = new XmlSerialization(messageType, stream, message, conventions, cache, SkipWrappingRawXml, Namespace))
            {
                serializer.Serialize();
            }
        }

        /// <summary>
        /// Scans the given type storing maps to fields and properties to save on reflection at runtime.
        /// </summary>
        public void InitType(Type t)
        {
            cache.InitType(t);
        }

        /// <summary>
        /// Initialized the serializer with the given message types.
        /// </summary>
        public void Initialize(IEnumerable<Type> types)
        {
            var messageTypes = types.ToList();

            if (!messageTypes.Contains(typeof(EncryptedValue)))
            {
                messageTypes.Add(typeof(EncryptedValue));
            }

            foreach (var t in messageTypes)
            {
                cache.InitType(t);
            }
        }

        string TrimPotentialTrailingForwardSlashes(string value)
        {
            return value?.TrimEnd('/');
        }

        XmlSerializerCache cache = new XmlSerializerCache();

        Conventions conventions;
        IMessageMapper mapper;

        string nameSpace = "http://tempuri.net";
    }
}