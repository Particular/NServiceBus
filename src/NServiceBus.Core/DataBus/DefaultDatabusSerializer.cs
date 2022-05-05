namespace NServiceBus
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using DataBus;

#pragma warning disable IDE0079
#pragma warning disable SYSLIB0011
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
#pragma warning restore SYSLIB0011
#pragma warning restore IDE0079
}