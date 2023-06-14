#nullable enable
namespace NServiceBus.Serializers.SystemJson
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Serialization;

    class JsonMessageSerializer : IMessageSerializer
    {
        internal JsonMessageSerializer(SystemJsonSerializerSettings settings, IMessageMapper messageMapper)
            : this(settings.SerializerOptions, settings.WriterOptions, settings.ReaderOptions, settings.ContentType, messageMapper)
        {
        }

        public JsonMessageSerializer(JsonSerializerOptions? serializerOptions, JsonWriterOptions writerOptions, JsonReaderOptions readerOptions, string contentType, IMessageMapper messageMapper)
        {
            this.serializerOptions = serializerOptions;
            this.writerOptions = writerOptions;
            this.readerOptions = readerOptions;
            this.messageMapper = messageMapper;

            ContentType = contentType;
        }

        public string ContentType { get; }

        public void Serialize(object message, Stream stream)
        {
            using var writer = new Utf8JsonWriter(stream, writerOptions);
            JsonSerializer.Serialize(writer, message, serializerOptions);
        }

        public object[] Deserialize(ReadOnlyMemory<byte> body, IList<Type>? messageTypes = null)
        {
            if (messageTypes == null || messageTypes.Count == 0)
            {
                throw new("The System.Text.Json message serializer requires message types to be defined.");
            }

            if (messageTypes.Count == 1)
            {
                return new[] { Deserialize(body, messageTypes[0]) };
            }

            var rootTypes = FindRootTypes(messageTypes);
            return rootTypes.Select(rootType => Deserialize(body, rootType))
                .ToArray();
        }

        object Deserialize(ReadOnlyMemory<byte> body, Type type)
        {
            var actualType = GetMappedType(type);

            // Get rid of the BOM if present since system json can't handle it
            if (body.Span.StartsWith(utf8Preamble))
            {
                body = body.Slice(utf8Preamble.Length);
            }

            var reader = new Utf8JsonReader(body.Span, readerOptions);
            return JsonSerializer.Deserialize(ref reader, actualType, serializerOptions)!;
        }

        static IEnumerable<Type> FindRootTypes(IEnumerable<Type> messageTypesToDeserialize)
        {
            Type? currentRoot = null;
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

        Type GetMappedType(Type messageType)
        {
            if (messageType.IsInterface)
            {
                var mappedTypeFor = messageMapper.GetMappedTypeFor(messageType);
                if (mappedTypeFor != null)
                {
                    return mappedTypeFor;
                }
            }
            return messageType;
        }

        readonly JsonSerializerOptions? serializerOptions;
        readonly JsonWriterOptions writerOptions;
        readonly JsonReaderOptions readerOptions;
        readonly IMessageMapper messageMapper;

        static readonly byte[] utf8Preamble = Encoding.UTF8.GetPreamble();
    }
}
