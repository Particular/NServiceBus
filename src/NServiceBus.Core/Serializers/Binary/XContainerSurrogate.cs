namespace NServiceBus.Serializers.Binary
{
    using System.IO;
    using System.Runtime.Serialization;
    using System.Xml.Linq;

    class XContainerSurrogate : ISerializationSurrogate
    {
        private const string FieldName = "_XDocument";

        public void GetObjectData(object obj, SerializationInfo info, StreamingContext context)
        {
            var document = (XDocument)obj;
            info.AddValue(FieldName, document.ToString(SaveOptions.DisableFormatting));
        }

        public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector selector)
        {
            return XDocument.Load(new StringReader(info.GetString(FieldName)));
        }
    }
}