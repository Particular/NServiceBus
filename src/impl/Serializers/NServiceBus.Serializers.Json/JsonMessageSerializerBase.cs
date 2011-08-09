using System.IO;
using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;
using NServiceBus.MessageInterfaces;
using NServiceBus.Serialization;
using NServiceBus.Serializers.Json.Internal;

namespace NServiceBus.Serializers.Json
{
  public abstract class JsonMessageSerializerBase : IMessageSerializer
  {
    private readonly IMessageMapper messageMapper;

    protected JsonMessageSerializerBase(IMessageMapper messageMapper)
    {
      this.messageMapper = messageMapper;
    }

    public void Serialize(IMessage[] messages, Stream stream)
    {
      var jsonSerializer = CreateJsonSerializer();

      var jsonWriter = CreateJsonWriter(stream);
      
      jsonSerializer.Serialize(jsonWriter, messages);

      jsonWriter.Flush();
    }

    public IMessage[] Deserialize(Stream stream)
    {
      var jsonSerializer = CreateJsonSerializer();

      var reader = CreateJsonReader(stream);
      
      var messages = jsonSerializer.Deserialize<IMessage[]>(reader);

      return messages;
    }

    private JsonSerializer CreateJsonSerializer()
    {
      var serializerSettings = new JsonSerializerSettings
      {
        TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
        TypeNameHandling = TypeNameHandling.Objects
      };

      serializerSettings.Converters.Add(new MessageJsonConverter(messageMapper));
     
      return JsonSerializer.Create(serializerSettings);
    }

    protected abstract JsonWriter CreateJsonWriter(Stream stream);
    
    protected abstract JsonReader CreateJsonReader(Stream stream);
  }
}