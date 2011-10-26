namespace NServiceBus.Unicast.Subscriptions.Raven
{
    using System;
    using Newtonsoft.Json;

    public class Subscription
    {
        public string Id { get; set; }

        [JsonConverter(typeof(MessageTypeConverter))]
        public MessageType MessageType { get; set; }

        public Address Client { get; set; }

        public static string FormatId(string endpoint, MessageType messageType, string client)
        {
            return string.Format("Subscriptions/{0}/{1}/{2}/{3}", endpoint, messageType.TypeName,messageType.Version, client);
        }
    }

    public class MessageTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(MessageType));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {

            return new MessageType(serializer.Deserialize<string>(reader));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.ToString());
        }
    }
}