namespace NServiceBus.Serializers.Json
{
    using System;
    using System.IO;
    using System.Text;
    using MessageInterfaces;
    using Newtonsoft.Json;

    class JsonMessageSerializer : JsonMessageSerializerBase
    {
        Encoding encoding = Encoding.UTF8;

        public JsonMessageSerializer(IMessageMapper messageMapper)
            : base(messageMapper)
        {
        }

        protected internal override JsonWriter CreateJsonWriter(Stream stream)
        {
            var streamWriter = new StreamWriter(stream, Encoding);
            return new JsonTextWriter(streamWriter) {Formatting = Formatting.None};
        }

        protected internal override JsonReader CreateJsonReader(Stream stream)
        {
            var streamReader = new StreamReader(stream, Encoding);
            return new JsonTextReader(streamReader);
        }

        public object DeserializeObject(string value, Type type)
        {
            return JsonConvert.DeserializeObject(value, type);
        }

        public string SerializeObject(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        protected internal override string GetContentType()
        {
            return ContentTypes.Json;
        }

        public Encoding Encoding
        {
            get { return encoding; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                encoding = value;
            }
        }
    }
}