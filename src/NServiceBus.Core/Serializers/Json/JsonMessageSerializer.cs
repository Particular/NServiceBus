namespace NServiceBus.Serializers.Json
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters;
    using System.Text;
    using MessageInterfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using NServiceBus.Serialization;

    /// <summary>
    /// JSON message serializer.
    /// </summary>
    public class JsonMessageSerializer : IMessageSerializer
    {
        IMessageMapper messageMapper;
        JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters =
            {
                new IsoDateTimeConverter
                {
                    DateTimeStyles = DateTimeStyles.RoundtripKind
                },
                new XContainerConverter()
            }
        };
        Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonMessageSerializer"/>.
        /// </summary>
        public JsonMessageSerializer(IMessageMapper messageMapper)
        {
            this.messageMapper = messageMapper;
        }


        /// <summary>
        /// Serializes the given set of messages into the given stream.
        /// </summary>
        /// <param name="message">Message to serialize.</param>
        /// <param name="stream">Stream for <paramref name="message"/> to be serialized into.</param>
        public void Serialize(object message, Stream stream)
        {
            Guard.AgainstNull("stream", stream);
            Guard.AgainstNull("message", message);
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
        public object[] Deserialize(Stream stream, IList<Type> messageTypes)
        {
            Guard.AgainstNull("stream", stream);
            var settings = serializerSettings;

            var mostConcreteType = messageTypes?.FirstOrDefault();
            var requiresDynamicDeserialization = mostConcreteType != null && mostConcreteType.IsInterface;

            if (requiresDynamicDeserialization)
            {
                settings = new JsonSerializerSettings
                {
                    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                    TypeNameHandling = TypeNameHandling.None,
                    Converters =
                    {
                        new IsoDateTimeConverter
                        {
                            DateTimeStyles = DateTimeStyles.RoundtripKind
                        },
                        new XContainerConverter()
                    }
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
                    //We can safely use the first type on the list to create an array because multi-message Publish requires messages to be of same type.
                    return (object[])jsonSerializer.Deserialize(reader, mostConcreteType.MakeArrayType());
                }
                return jsonSerializer.Deserialize<object[]>(reader);
            }
            if (messageTypes != null && messageTypes.Any())
            {
                var rootTypes = FindRootTypes(messageTypes);
                return rootTypes.Select(x =>
                {
                    if (reader == null)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        reader = CreateJsonReader(stream);
                        reader.Read();
                    }
                    var deserialized = jsonSerializer.Deserialize(reader, x);
                    reader = null;
                    return deserialized;
                }).ToArray();
            }
            return new[]
            {
                jsonSerializer.Deserialize<object>(reader)
            };
        }

        static IEnumerable<Type> FindRootTypes(IEnumerable<Type> messageTypesToDeserialize)
        {
            Type currentRoot = null;
            foreach (var type in messageTypesToDeserialize)
            {
                if (currentRoot == null)
                {
                    currentRoot = type;
                    yield return currentRoot;
                    continue;
                }
                if (!type.IsAssignableFrom(currentRoot))
                {
                    currentRoot = type;
                    yield return currentRoot;
                }
            }
        }

        JsonWriter CreateJsonWriter(Stream stream)
        {
            var streamWriter = new StreamWriter(stream, Encoding);
            return new JsonTextWriter(streamWriter) {Formatting = Formatting.None};
        }

        JsonReader CreateJsonReader(Stream stream)
        {
            var streamReader = new StreamReader(stream, Encoding);
            return new JsonTextReader(streamReader);
        }

        /// <summary>
        /// Non strongly typed deserialization.
        /// </summary>
        public object DeserializeObject(string value, Type type)
        {
            Guard.AgainstNull("type", type);
            Guard.AgainstNullAndEmpty("value", value);
            return JsonConvert.DeserializeObject(value, type);
        }

        /// <summary>
        /// Serializes the given object to a json string.
        /// </summary>
        /// <param name="value">The actual object.</param>
        /// <returns>The json string.</returns>
        public string SerializeObject(object value)
        {
            Guard.AgainstNull("value", value);
            return JsonConvert.SerializeObject(value);
        }
        

        /// <summary>
        /// Gets or sets the stream encoding.
        /// </summary>
        public Encoding Encoding
        {
            get { return encoding; }
            set
            {
                Guard.AgainstNull("value", value);
                encoding = value;
            }
        }

        /// <summary>
        /// Gets the content type into which this serializer serializes the content to.
        /// </summary>
        public string ContentType => ContentTypes.Json;
    }
}