namespace NServiceBus.Serializers.Json
{
    using System.Globalization;
    using System.IO;
    using System.Runtime.Serialization.Formatters;

    using Internal;
    using MessageInterfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Serialization;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
        public bool SkipArrayWrappingForSingleMessages { get; set; }

        /// <summary>
        /// Serializes the given set of messages into the given stream.
        /// </summary>
        /// <param name="messages">Messages to serialize.</param>
        /// <param name="stream">Stream for <paramref name="messages"/> to be serialized into.</param>
        public void Serialize(object[] messages, Stream stream)
        {
            var jsonSerializer = JsonSerializer.Create(serializerSettings);
            jsonSerializer.Binder = new MessageSerializationBinder(messageMapper);

            var jsonWriter = CreateJsonWriter(stream);

            if (SkipArrayWrappingForSingleMessages && messages.Length == 1)
                jsonSerializer.Serialize(jsonWriter, messages[0]);
            else
                jsonSerializer.Serialize(jsonWriter, messages);

            jsonWriter.Flush();
        }

        /// <summary>
        /// Deserializes from the given stream a set of messages.
        /// </summary>
        /// <param name="stream">Stream that contains messages.</param>
        /// <param name="messageTypes">The list of message types to deserialize. If null the types must be infered from the serialized data.</param>
        /// <returns>Deserialized messages.</returns>
        public object[] Deserialize(Stream stream, IList<string> messageTypes = null)
        {
            var jsonSerializer = JsonSerializer.Create(serializerSettings);
            jsonSerializer.ContractResolver = new MessageContractResolver(messageMapper);

            var reader = CreateJsonReader(stream);
            reader.Read();

            var firstTokenType = reader.TokenType;

            if (firstTokenType == JsonToken.StartArray)
            {
                return jsonSerializer.Deserialize<object[]>(reader);
            }

            return new[] {jsonSerializer.Deserialize(reader, Type.GetType(messageTypes.First()))};
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
