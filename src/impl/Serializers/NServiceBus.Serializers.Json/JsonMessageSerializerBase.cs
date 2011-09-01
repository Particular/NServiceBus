using System.IO;
using System.Runtime.Serialization.Formatters;
using NServiceBus.MessageInterfaces;
using NServiceBus.Serialization;
using NServiceBus.Serializers.Json.Internal;
using Newtonsoft.Json;

namespace NServiceBus.Serializers.Json
{
    public abstract class JsonMessageSerializerBase : IMessageSerializer
    {
        private readonly IMessageMapper messageMapper;

        protected JsonMessageSerializerBase(IMessageMapper messageMapper)
        {
            this.messageMapper = messageMapper;
        }

        public static JsonSerializerSettings JsonSerializerSettings
        {
            get
            {
                var serializerSettings = new JsonSerializerSettings
                                             {
                                                 TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                                                 TypeNameHandling = TypeNameHandling.Objects
                                             };
                return serializerSettings;
            }
        }

        public void Serialize(object[] messages, Stream stream)
        {
            JsonSerializer jsonSerializer = CreateJsonSerializer();

            JsonWriter jsonWriter = CreateJsonWriter(stream);

            jsonSerializer.Serialize(jsonWriter, messages);

            jsonWriter.Flush();
        }

        public object[] Deserialize(Stream stream)
        {
            JsonSerializer jsonSerializer = CreateJsonSerializer();

            JsonReader reader = CreateJsonReader(stream);

            var messages = jsonSerializer.Deserialize<IMessage[]>(reader);

            return messages;
        }

        private JsonSerializer CreateJsonSerializer()
        {
            JsonSerializerSettings serializerSettings = JsonSerializerSettings;

            serializerSettings.Converters.Add(new MessageJsonConverter(messageMapper));

            return JsonSerializer.Create(serializerSettings);
        }

        protected abstract JsonWriter CreateJsonWriter(Stream stream);

        protected abstract JsonReader CreateJsonReader(Stream stream);
    }
}