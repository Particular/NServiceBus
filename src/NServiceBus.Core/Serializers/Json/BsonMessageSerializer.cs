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
            wrapMessagesInArray = true;
        }

        /// <summary>
        /// Creates the writer
        /// </summary>
        /// <param name="stream"></param>
        protected internal override JsonWriter CreateJsonWriter(Stream stream)
        {
            return new BsonWriter(stream);
        }

        /// <summary>
        /// Creates the reader
        /// </summary>
        /// <param name="stream"></param>
        protected internal override JsonReader CreateJsonReader(Stream stream)
        {
            return new BsonReader(stream, true, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// Gets the supported content type
        /// </summary>
        protected internal override string GetContentType()
        {
            return ContentTypes.Bson;
        }
    }
}