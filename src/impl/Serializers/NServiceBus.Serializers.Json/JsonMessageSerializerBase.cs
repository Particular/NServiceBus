using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters;
using NServiceBus.MessageInterfaces;
using NServiceBus.Serialization;
using NServiceBus.Serializers.Json.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace NServiceBus.Serializers.Json
{
    public abstract class JsonMessageSerializerBase : IMessageSerializer
    {
        private readonly IMessageMapper _messageMapper;

        protected JsonMessageSerializerBase(IMessageMapper messageMapper)
        {
            _messageMapper = messageMapper;
        }

        public JsonSerializerSettings JsonSerializerSettings
        {
            get
            {
                var serializerSettings = new JsonSerializerSettings
                                             {
                                                 TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                                                 TypeNameHandling = TypeNameHandling.Auto,
                                                 Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.RoundtripKind} }
                                             };
                return serializerSettings;
            }
        }

        public void Serialize(object[] messages, Stream stream)
        {
            JsonSerializer jsonSerializer = JsonSerializer.Create(JsonSerializerSettings);
            jsonSerializer.Binder = new MessageSerializationBinder(_messageMapper);

            JsonWriter jsonWriter = CreateJsonWriter(stream);

            jsonSerializer.Serialize(jsonWriter, messages);
            jsonWriter.Flush();
        }

        public object[] Deserialize(Stream stream)
        {
            JsonSerializer jsonSerializer = JsonSerializer.Create(JsonSerializerSettings);
            jsonSerializer.ContractResolver = new MessageContractResolver(_messageMapper);

            JsonReader reader = CreateJsonReader(stream);

            return jsonSerializer.Deserialize<object[]>(reader);
        }

        protected abstract JsonWriter CreateJsonWriter(Stream stream);

        protected abstract JsonReader CreateJsonReader(Stream stream);
    }
}