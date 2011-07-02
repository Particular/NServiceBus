using System.IO;
using System.Text;
using Newtonsoft.Json;
using NServiceBus.MessageInterfaces;

namespace NServiceBus.Serializers.Json
{
  public class JsonMessageSerializer : JsonMessageSerializerBase
  {
    public JsonMessageSerializer(IMessageMapper messageMapper) : base(messageMapper)
    {
    }

    protected override JsonWriter CreateJsonWriter(Stream stream)
    {
      var streamWriter = new StreamWriter(stream, Encoding.UTF8);
      return new JsonTextWriter(streamWriter) { Formatting = Formatting.Indented };
    }

    protected override JsonReader CreateJsonReader(Stream stream)
    {
      var streamReader = new StreamReader(stream, Encoding.UTF8);
      return new JsonTextReader(streamReader);
    }
  }
}