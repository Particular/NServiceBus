namespace NServiceBus
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using NServiceBus.DataBus;

    class DefaultDataBusSerializer : IDataBusSerializer
	{
		static BinaryFormatter formatter = new BinaryFormatter();
      
		public void Serialize(object databusProperty, Stream stream)
		{
			formatter.Serialize(stream, databusProperty);
		}

		public object Deserialize(Stream stream)
		{
			return formatter.Deserialize(stream);
		}
	}
}