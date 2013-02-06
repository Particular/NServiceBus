namespace NServiceBus.Serializers.Json
{
    using System;
    using System.IO;
    using System.Text;
    using MessageInterfaces;
    using Newtonsoft.Json;

    /// <summary>
    /// JSON message serializer.
    /// </summary>
    public class JsonMessageSerializer : JsonMessageSerializerBase
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="messageMapper"></param>
        public JsonMessageSerializer(IMessageMapper messageMapper)
            : base(messageMapper)
        {
        }

        protected override JsonWriter CreateJsonWriter(Stream stream)
        {
            var streamWriter = new StreamWriter(stream, Encoding.UTF8);
            return new JsonTextWriter(streamWriter) {Formatting = Formatting.None};
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

        public object DeserializeObject(string value, Type type)
        {
            return JsonConvert.DeserializeObject(value, type);
        }

        public string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        protected override string GetContentType()
        {
            return ContentTypes.Json;
        }
    }
}