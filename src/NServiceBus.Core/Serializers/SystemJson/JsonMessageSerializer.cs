#nullable enable
namespace NServiceBus.Serializers.SystemJson
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using NServiceBus.Serialization;

    class JsonMessageSerializer : IMessageSerializer
    {
        internal JsonMessageSerializer(SystemJsonSerializerSettings s)
            : this(s.SerializerOptions, s.WriterOptions, s.ReaderOptions, s.ContentType)
        {
        }

        public JsonMessageSerializer(JsonSerializerOptions? serializerOptions, JsonWriterOptions writerOptions, JsonReaderOptions readerOptions, string contentType)
        {
            this.serializerOptions = serializerOptions;
            this.writerOptions = writerOptions;
            this.readerOptions = readerOptions;

            ContentType = contentType;
        }

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
            var reader = new Utf8JsonReader(body.Span, readerOptions);
            return JsonSerializer.Deserialize(ref reader, type, serializerOptions)!;
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

        public string ContentType { get; }

        JsonSerializerOptions? serializerOptions;
        JsonWriterOptions writerOptions;
        JsonReaderOptions readerOptions;
    }
}
