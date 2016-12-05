namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters;
    using System.Text;
    using MessageInterfaces;
    using Newtonsoft.Json;
    using Serialization;

    class JsonMessageSerializer : IMessageSerializer
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonMessageSerializer" />.
        /// </summary>
        public JsonMessageSerializer(IMessageMapper messageMapper, Encoding encoding)
        {
            this.messageMapper = messageMapper;
            this.encoding = encoding;

            messageContractResolver = new MessageContractResolver(messageMapper);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="JsonMessageSerializer" />.
        /// </summary>
        public JsonMessageSerializer(IMessageMapper messageMapper)
            : this(messageMapper, Encoding.UTF8)
        {
        }


        /// <summary>
        /// Gets or sets the stream encoding.
        /// </summary>
        public Encoding Encoding
        {
            get { return encoding; }
            set
            {
                Guard.AgainstNull(nameof(value), value);
                encoding = value;
            }
        }

        /// <summary>
        /// Serializes the given set of messages into the given stream.
        /// </summary>
        /// <param name="message">Message to serialize.</param>
        /// <param name="stream">Stream for <paramref name="message" /> to be serialized into.</param>
        public void Serialize(object message, Stream stream)
        {
            Guard.AgainstNull(nameof(stream), stream);
            Guard.AgainstNull(nameof(message), message);
            var jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(serializerSettings);
            jsonSerializer.Binder = new JsonMessageSerializationBinder(messageMapper);

            var jsonWriter = CreateJsonWriter(stream);
            jsonSerializer.Serialize(jsonWriter, message);
            jsonWriter.Flush();
        }

        /// <summary>
        /// Deserializes from the given stream a set of messages.
        /// </summary>
        /// <param name="stream">Stream that contains messages.</param>
        /// <param name="messageTypes">
        /// The list of message types to deserialize. If null the types must be inferred from the
        /// serialized data.
        /// </param>
        /// <returns>Deserialized messages.</returns>
        public object[] Deserialize(Stream stream, IList<Type> messageTypes)
        {
            Guard.AgainstNull(nameof(stream), stream);
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
                        new XContainerJsonConverter()
                    }
                };
            }

            var jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(settings);
            jsonSerializer.ContractResolver = messageContractResolver;
            jsonSerializer.Binder = new JsonMessageSerializationBinder(messageMapper, messageTypes);

            var reader = CreateJsonReader(stream);
            reader.Read();

            var firstTokenType = reader.TokenType;

            if (firstTokenType == JsonToken.StartArray)
            {
                if (requiresDynamicDeserialization)
                {
                    //We can safely use the first type on the list to create an array because multi-message Publish requires messages to be of same type.
                    return (object[]) jsonSerializer.Deserialize(reader, mostConcreteType.MakeArrayType());
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

        /// <summary>
        /// Gets the content type into which this serializer serializes the content to.
        /// </summary>
        public string ContentType => ContentTypes.Json;

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
            return new JsonTextWriter(streamWriter)
            {
                Formatting = Formatting.None
            };
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
            Guard.AgainstNull(nameof(type), type);
            Guard.AgainstNullAndEmpty(nameof(value), value);
            return JsonConvert.DeserializeObject(value, type);
        }

        /// <summary>
        /// Serializes the given object to a json string.
        /// </summary>
        /// <param name="value">The actual object.</param>
        /// <returns>The json string.</returns>
        public string SerializeObject(object value)
        {
            Guard.AgainstNull(nameof(value), value);
            return JsonConvert.SerializeObject(value);
        }

        Encoding encoding;

        MessageContractResolver messageContractResolver;
        IMessageMapper messageMapper;

        JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
            TypeNameHandling = TypeNameHandling.Auto,
            Converters =
            {
                new XContainerJsonConverter()
            }
        };
    }
}