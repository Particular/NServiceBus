namespace NServiceBus
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using DataBus;

    class DefaultDataBusSerializer : IDataBusSerializer
    {
        public void Serialize(object databusProperty, Stream stream)
        {
            formatter.Serialize(stream, databusProperty);
        }

        public object Deserialize(Stream stream)
        {
            return formatter.Deserialize(stream);
        }

        static BinaryFormatter formatter = new BinaryFormatter();
    }
}