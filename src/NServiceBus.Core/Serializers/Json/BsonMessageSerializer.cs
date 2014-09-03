namespace NServiceBus.Serializers.Json
{
    using System;
    using System.IO;
    using MessageInterfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Bson;

    class BsonMessageSerializer : JsonMessageSerializerBase
    {
        public BsonMessageSerializer(IMessageMapper messageMapper)
            : base(messageMapper)
        {
            wrapMessagesInArray = true;
        }

        protected internal override JsonWriter CreateJsonWriter(Stream stream)
        {
            return new BsonWriter(stream);
        }
        protected internal override JsonReader CreateJsonReader(Stream stream)
        {
            return new BsonReader(stream, true, DateTimeKind.Unspecified);
        }

        protected internal override string GetContentType()
        {
            return ContentTypes.Bson;
        }
    }
}