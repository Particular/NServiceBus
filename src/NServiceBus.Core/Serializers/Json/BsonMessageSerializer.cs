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
        /// Constructor
        /// </summary>
        public BsonMessageSerializer(IMessageMapper messageMapper): base(messageMapper)
        {
        }

        protected override JsonWriter CreateJsonWriter(Stream stream)
        {
            return new BsonWriter(stream);
        }

        protected override JsonReader CreateJsonReader(Stream stream)
        {
            return new BsonReader(stream, true, DateTimeKind.Unspecified);
        }

        protected override string GetContentType()
        {
            return ContentTypes.Bson;
        }
    }
}