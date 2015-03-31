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
        private Encoding encoding = Encoding.UTF8;

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
            var streamWriter = new StreamWriter(stream, Encoding);
            return new JsonTextWriter(streamWriter) {Formatting = Formatting.None};
        }

        /// <summary>
        /// Creates the reader
        /// </summary>
        /// <param name="stream"></param>
        protected internal override JsonReader CreateJsonReader(Stream stream)
        {
            var streamReader = new StreamReader(stream, Encoding);
            return new JsonTextReader(streamReader);
        }

        /// <summary>
        /// Non strongly typed deserialization
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        public object DeserializeObject(string value, Type type)
        {
            Guard.AgainstNull(type, "type");
            Guard.AgainstNullAndEmpty(value, "value");
            return JsonConvert.DeserializeObject(value, type);
        }

        /// <summary>
        /// Serializes the given object to a json string
        /// </summary>
        /// <param name="value">The actual object</param>
        /// <returns>The json string</returns>
        public string SerializeObject(object value)
        {
            Guard.AgainstNull(value, "value");
            return JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// Returns the supported content type
        /// </summary>
        protected internal override string GetContentType()
        {
            return ContentTypes.Json;
        }

        /// <summary>
        /// Gets or sets the stream encoding
        /// </summary>
        public Encoding Encoding
        {
            get { return encoding; }
            set
            {
                Guard.AgainstNull(value, "value");
                encoding = value;
            }
        }
    }
}