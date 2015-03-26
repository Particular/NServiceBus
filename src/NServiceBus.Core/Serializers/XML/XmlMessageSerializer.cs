namespace NServiceBus.Serializers.XML
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Serialization;

    /// <summary>
    ///     Implementation of the message serializer over XML supporting interface-based messages.
    /// </summary>
    public class XmlMessageSerializer : IMessageSerializer
    {
        /// <summary>
        ///     Initializes an instance of a <see cref="XmlMessageSerializer" />.
        /// </summary>
        /// <param name="mapper">Message Mapper</param>
        /// <param name="conventions">The endpoint conventions.</param>
        public XmlMessageSerializer(IMessageMapper mapper, Conventions conventions)
        {
            Guard.AgainstNull(mapper, "mapper");
            Guard.AgainstNull(conventions, "conventions");
            this.mapper = mapper;
            this.conventions = conventions;
        }

        /// <summary>
        ///     The namespace to place in outgoing XML.
        ///     <para>If the provided namespace ends with trailing forward slashes, those will be removed on the fly.</para>
        /// </summary>
        public string Namespace
        {
            get { return nameSpace; }
            set { nameSpace = TrimPotentialTrailingForwardSlashes(value); }
        }

        /// <summary>
        ///     If true, then the serializer will use a sanitizing stream to skip invalid characters from the stream before parsing
        /// </summary>
        public bool SanitizeInput { get; set; }

        /// <summary>
        ///     Removes the wrapping of properties containing XDocument or XElement with property name as root element
        /// </summary>
        public bool SkipWrappingRawXml { get; set; }

        /// <summary>
        ///     Deserializes from the given stream a set of messages.
        /// </summary>
        /// <param name="stream">Stream that contains messages.</param>
        /// <param name="messageTypesToDeserialize">
        ///     The list of message types to deserialize. If null the types must be inferred
        ///     from the serialized data.
        /// </param>
        /// <returns>Deserialized messages.</returns>
        public object[] Deserialize(Stream stream, IList<Type> messageTypesToDeserialize = null)
        {
            if (stream == null)
            {
                return null;
            }

            var deserializer = new Deserializer(mapper, cache, SkipWrappingRawXml, SanitizeInput);
            return deserializer.Deserialize(stream, messageTypesToDeserialize);
        }

        /// <summary>
        ///     Serializes the given messages to the given stream.
        /// </summary>
        public void Serialize(object message, Stream stream)
        {
            using (var serializer = new Serializer(mapper, stream, message, conventions, cache, SkipWrappingRawXml, Namespace))
            {
                serializer.Serialize();
            }
        }

        /// <summary>
        ///     Supported content type
        /// </summary>
        public string ContentType
        {
            get { return ContentTypes.Xml; }
        }

        /// <summary>
        ///     Scans the given type storing maps to fields and properties to save on reflection at runtime.
        /// </summary>
        public void InitType(Type t)
        {
            cache.InitType(t);
        }

        /// <summary>
        ///     Initialized the serializer with the given message types
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
            if (value == null)
            {
                return null;
            }

            return value.TrimEnd(new[]
            {
                '/'
            });
        }

        readonly Conventions conventions;
        readonly IMessageMapper mapper;

        XmlSerializerCache cache = new XmlSerializerCache();

        string nameSpace = "http://tempuri.net";
    }
}