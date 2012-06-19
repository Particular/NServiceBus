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
      return new JsonTextWriter(streamWriter) { Formatting = Formatting.None };
    }

    protected override JsonReader CreateJsonReader(Stream stream)
    {
      var streamReader = new StreamReader(stream, Encoding.UTF8);
      return new JsonTextReader(streamReader);
    }

    public T DeserializeObject<T>(string value)
    {
        return JsonConvert.DeserializeObject<T>(value);
    }

    public string SerializeObject(object value)
    {
        return JsonConvert.SerializeObject(value);
    }
  }
}