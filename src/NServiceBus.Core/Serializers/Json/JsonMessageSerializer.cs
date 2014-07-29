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
        public JsonMessageSerializer(IMessageMapper messageMapper)
            : base(messageMapper)
        {
        }

        /// <summary>
        /// Creates the writer
        /// </summary>
        /// <param name="stream"></param>
        protected internal override JsonWriter CreateJsonWriter(Stream stream)
        {
            var streamWriter = new StreamWriter(stream, Encoding.UTF8);
            return new JsonTextWriter(streamWriter) {Formatting = Formatting.None};
        }

        /// <summary>
        /// Creates the reader
        /// </summary>
        /// <param name="stream"></param>
        protected internal override JsonReader CreateJsonReader(Stream stream)
        {
            var streamReader = new StreamReader(stream, Encoding.UTF8);
            return new JsonTextReader(streamReader);
        }

        /// <summary>
        /// Non strongly typed deserialization
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        public object DeserializeObject(string value, Type type)
        {
            return JsonConvert.DeserializeObject(value, type);
        }

        /// <summary>
        /// Serializes the given object to a json string
        /// </summary>
        /// <param name="value">The actual object</param>
        /// <returns>The json string</returns>
        public string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// Returns the supported content type
        /// </summary>
        protected internal override string GetContentType()
        {
            return ContentTypes.Json;
        }
    }
}