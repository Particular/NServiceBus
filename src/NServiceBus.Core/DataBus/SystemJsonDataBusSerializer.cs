namespace NServiceBus
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using DataBus;

#pragma warning disable IDE0079
#pragma warning disable SYSLIB0011
    /// <summary>
    /// Data bus serialization using the System.Text.Json serializer. />.
    /// </summary>
    public class SystemJsonDataBusSerializer : IDataBusSerializer
    {
        /// <summary>
        /// Serializes the property.
        /// </summary>
        public void Serialize(object databusProperty, Stream stream)
        {
            formatter.Serialize(stream, databusProperty);
        }

        /// <summary>
        /// Deserializes the property.
        /// </summary>
        public object Deserialize(Stream stream)
        {
            return formatter.Deserialize(stream);
        }

        static BinaryFormatter formatter = new BinaryFormatter();
    }
#pragma warning restore SYSLIB0011
#pragma warning restore IDE0079
}