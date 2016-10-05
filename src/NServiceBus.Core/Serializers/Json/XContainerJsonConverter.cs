namespace NServiceBus
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Xml.Linq;
    using Newtonsoft.Json;

    class XContainerJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }

            var container = (XContainer) value;

            writer.WriteValue(container.ToString(SaveOptions.DisableFormatting));
        }

        public override object ReadJson(
            JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.String)
            {
                throw new Exception(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Unexpected token or value when parsing XContainer. Token: {0}, Value: {1}",
                        reader.TokenType,
                        reader.Value));
            }

            var value = (string) reader.Value;
            if (objectType == typeof(XDocument))
            {
                try
                {
                    return XDocument.Load(new StringReader(value));
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        string.Format(
                            CultureInfo.InvariantCulture, "Error parsing XContainer string: {0}", reader.Value),
                        ex);
                }
            }

            return XElement.Load(new StringReader(value));
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(XContainer).IsAssignableFrom(objectType);
        }
    }
}