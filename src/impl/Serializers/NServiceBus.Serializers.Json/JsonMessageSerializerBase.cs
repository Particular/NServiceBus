using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NServiceBus.MessageInterfaces;
using NServiceBus.Serialization;
using NServiceBus.Serializers.Json.Internal;

namespace NServiceBus.Serializers.Json
{
    public abstract class JsonMessageSerializerBase : IMessageSerializer
    {
        // From v4+ we just look for it, but don't expect it
        const string EnclosedMessageTypes = "NServiceBus.EnclosedMessageTypes";

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
                                                 Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.RoundtripKind } }
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
            reader.Read();

            JsonToken firstTokenType = reader.TokenType;

            if (firstTokenType != JsonToken.StartArray)
            {
                string messageTypes;

                var bus = Configure.Instance.Builder.Build<IBus>();
                if (bus != null &&
                    bus.CurrentMessageContext != null &&
                    bus.CurrentMessageContext.Headers != null &&
                    bus.CurrentMessageContext.Headers.TryGetValue(EnclosedMessageTypes, out messageTypes))
                {
                    return new[] { jsonSerializer.Deserialize(reader, Type.GetType(messageTypes.Split(';')[0])) };
                }
            }

            return jsonSerializer.Deserialize<object[]>(reader);
        }

        protected abstract JsonWriter CreateJsonWriter(Stream stream);

        protected abstract JsonReader CreateJsonReader(Stream stream);
    }
}