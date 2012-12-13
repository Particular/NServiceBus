namespace NServiceBus.Serializers.Json
{
    using System.Globalization;
    using System.IO;
    using System.Runtime.Serialization.Formatters;
    using Internal;
    using MessageInterfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Bson;
    using Newtonsoft.Json.Converters;
    using Serialization;
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

        /// <summary>
        /// Removes the wrapping array if serializing a single message 
        /// </summary>
        public bool SkipArrayWrappingForSingleMessages { get; set; }

        public void Serialize(object[] messages, Stream stream)
        {
            JsonSerializer jsonSerializer = JsonSerializer.Create(JsonSerializerSettings);
            jsonSerializer.Binder = new MessageSerializationBinder(_messageMapper);

            JsonWriter jsonWriter = CreateJsonWriter(stream);

            if(SkipArrayWrappingForSingleMessages && messages.Length == 1)
                jsonSerializer.Serialize(jsonWriter, messages[0]);
            else
                jsonSerializer.Serialize(jsonWriter, messages);
            jsonWriter.Flush();
        }

        public object[] Deserialize(Stream stream,IEnumerable<string> messageTypes = null)
        {
            JsonSerializer jsonSerializer = JsonSerializer.Create(JsonSerializerSettings);
            jsonSerializer.ContractResolver = new MessageContractResolver(_messageMapper);

            JsonReader reader = CreateJsonReader(stream);
            
            if(SkipArrayWrappingForSingleMessages && messageTypes != null && messageTypes.Count() == 1)
                return new[] { jsonSerializer.Deserialize(reader,Type.GetType(messageTypes.First())) };

            return jsonSerializer.Deserialize<object[]>(reader);
        }


        public string ContentType { get { return GetContentType(); } }

        protected abstract string GetContentType();

        protected abstract JsonWriter CreateJsonWriter(Stream stream);

        protected abstract JsonReader CreateJsonReader(Stream stream);
    }
}
