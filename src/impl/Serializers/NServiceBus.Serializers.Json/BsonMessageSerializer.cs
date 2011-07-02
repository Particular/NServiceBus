using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using NServiceBus.MessageInterfaces;

namespace NServiceBus.Serializers.Json
{
  public class BsonMessageSerializer : JsonMessageSerializerBase
  {
    public BsonMessageSerializer(IMessageMapper messageMapper) : base(messageMapper)
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
  }
}