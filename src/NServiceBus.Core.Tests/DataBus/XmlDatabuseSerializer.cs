namespace NServiceBus
{
    using System.IO;
    using DataBus;

    class XmlDataBusSerializer<T> : IDataBusSerializer
    {
        public void Serialize(object databusProperty, Stream stream)
        {
            formatter.Serialize(stream, databusProperty);
        }

        public object Deserialize(Stream stream)
        {
            return formatter.Deserialize(stream);
        }

        static System.Xml.Serialization.XmlSerializer formatter = new System.Xml.Serialization.XmlSerializer(typeof(T));
    }
}