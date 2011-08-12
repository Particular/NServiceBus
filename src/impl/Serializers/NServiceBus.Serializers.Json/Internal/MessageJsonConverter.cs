using System;
using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NServiceBus.MessageInterfaces;

namespace NServiceBus.Serializers.Json.Internal
{
  public class MessageJsonConverter : JsonConverter
  {
    private readonly IMessageMapper messageMapper;

    public MessageJsonConverter(IMessageMapper messageMapper)
    {
      this.messageMapper = messageMapper;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var mappedType = messageMapper.GetMappedTypeFor(value.GetType());
        var typeName = GetTypeName(mappedType);

        // the serializer settings need to be passed to correctly add type information to child objects
        // a new serializer is needed as this converter is otherwise called over and over again
        var s = JsonSerializer.Create(JsonMessageSerializerBase.JsonSerializerSettings);

        var jobj = JObject.FromObject(value, s);

        jobj.AddFirst(new JProperty("$messagetype", typeName));

        jobj.WriteTo(writer);
        
    }

    private static string GetTypeName(Type mappedType)
    {
      return string.Format("{0}, {1}", mappedType.FullName, mappedType.Assembly.GetName().Name);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      var jobject = JObject.Load(reader);

      var typeName = jobject.Value<string>("$messagetype");

      var type = Type.GetType(typeName);

      var instance = messageMapper.CreateInstance(type);

      serializer.Populate(jobject.CreateReader(), instance);

      return instance;
    }

    public override bool CanConvert(Type objectType)
    {
      return typeof(IMessage).IsAssignableFrom(objectType);
    }
  }
}