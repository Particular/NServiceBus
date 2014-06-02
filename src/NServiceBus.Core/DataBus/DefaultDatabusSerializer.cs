namespace NServiceBus.DataBus
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    class DefaultDataBusSerializer : IDataBusSerializer
	{
		private static readonly BinaryFormatter formatter = new BinaryFormatter();
      
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