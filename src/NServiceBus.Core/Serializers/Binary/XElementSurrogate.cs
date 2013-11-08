namespace NServiceBus.Serializers.Binary
{
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml.Linq;

    class XElementSurrogate : ISerializationSurrogate
    {
        private const string FieldName = "_XElement";

        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var document = (XElement)obj;
            info.AddValue(FieldName, document.ToString(SaveOptions.DisableFormatting));
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            return XElement.Load(new StringReader(info.GetString(FieldName)));
        }
    }
}