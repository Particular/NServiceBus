namespace NServiceBus.Serializers.Json
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters;
    using Internal;
    using MessageInterfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Serialization;

    /// <summary>
    /// JSON and BSON base class for <see cref="IMessageSerializer"/>.
    /// </summary>
    public abstract class JsonMessageSerializerBase : IMessageSerializer
    {
        private readonly IMessageMapper messageMapper;

        readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.RoundtripKind }, new XContainerConverter() }
        };

        protected JsonMessageSerializerBase(IMessageMapper messageMapper)
        {
            this.messageMapper = messageMapper;
        }
        
        /// <summary>
        /// Removes the wrapping array if serializing a single message 
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6.0", Message = "In version 5 multi-message sends was removed. So Wrapping messages is no longer required. If you are communicating with version 3 ensure you are on the latets 3.3.x.")]
        public bool SkipArrayWrappingForSingleMessages { get; set; }

        /// <summary>
        /// Serializes the given set of messages into the given stream.
        /// </summary>
        /// <param name="message">Message to serialize.</param>
        /// <param name="stream">Stream for <paramref name="message"/> to be serialized into.</param>
        public void Serialize(object message, Stream stream)
        {
            var jsonSerializer = JsonSerializer.Create(serializerSettings);
            jsonSerializer.Binder = new MessageSerializationBinder(messageMapper);

            var jsonWriter = CreateJsonWriter(stream);

            jsonSerializer.Serialize(jsonWriter, message);

            jsonWriter.Flush();
        }

        /// <summary>
        /// Deserializes from the given stream a set of messages.
        /// </summary>
        /// <param name="stream">Stream that contains messages.</param>
        /// <param name="messageTypes">The list of message types to deserialize. If null the types must be inferred from the serialized data.</param>
        /// <returns>Deserialized messages.</returns>
        public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
        {
            var settings = serializerSettings;

            var mostConcreteType = messageTypes != null ? messageTypes.FirstOrDefault() : null;
            var requiresDynamicDeserialization = mostConcreteType != null && mostConcreteType.IsInterface;

            if (requiresDynamicDeserialization)
            {
                settings = new JsonSerializerSettings{
                        TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                        TypeNameHandling = TypeNameHandling.None,
                        Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.RoundtripKind }, new XContainerConverter() }
                };
            }

            var jsonSerializer = JsonSerializer.Create(settings);
            jsonSerializer.ContractResolver = new MessageContractResolver(messageMapper);
            jsonSerializer.Binder = new MessageSerializationBinder(messageMapper, messageTypes);

            var reader = CreateJsonReader(stream);
            reader.Read();

            var firstTokenType = reader.TokenType;

            if (firstTokenType == JsonToken.StartArray)
            {
                if (requiresDynamicDeserialization)
                {
                    return (object[])jsonSerializer.Deserialize(reader, mostConcreteType.MakeArrayType());
                }
                return jsonSerializer.Deserialize<object[]>(reader);
            }
            if (messageTypes != null && messageTypes.Any())
            {
                return new[] {jsonSerializer.Deserialize(reader, messageTypes.First())};
            }

            return new[] {jsonSerializer.Deserialize<object>(reader)};
        }

        /// <summary>
        /// Gets the content type into which this serializer serializes the content to 
        /// </summary>
        public string ContentType { get { return GetContentType(); } }

        protected abstract string GetContentType();

        protected abstract JsonWriter CreateJsonWriter(Stream stream);

        protected abstract JsonReader CreateJsonReader(Stream stream);
    }
}
