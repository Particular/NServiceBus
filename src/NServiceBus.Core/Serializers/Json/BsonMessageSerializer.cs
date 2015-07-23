namespace NServiceBus.Serializers.Json
{
    using System;
    using System.IO;
    using MessageInterfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Bson;

    /// <summary>
    /// BSON message serializer.
    /// </summary>
    public class BsonMessageSerializer : JsonMessageSerializerBase
    {
        /// <summary>
        /// Initializes a new instance of <see cref="BsonMessageSerializer"/>.
        /// </summary>
        public BsonMessageSerializer(IMessageMapper messageMapper): base(messageMapper)
        {
            wrapMessagesInArray = true;
        }

        /// <inheritdoc />
        protected internal override JsonWriter CreateJsonWriter(Stream stream)
        {
            return new BsonWriter(stream);
        }

        /// <inheritdoc />
        protected internal override JsonReader CreateJsonReader(Stream stream)
        {
            return new BsonReader(stream, true, DateTimeKind.Unspecified);
        }

        /// <inheritdoc />
        protected internal override string GetContentType()
        {
            return ContentTypes.Bson;
        }
    }
}