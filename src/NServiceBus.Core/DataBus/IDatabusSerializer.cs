namespace NServiceBus.DataBus
{
    using System.IO;

    /// <summary>
	/// Interface used for serializing and deserializing of databus properties.
	/// </summary>
	public interface IDataBusSerializer
	{
		/// <summary>
		/// Serializes the property into the given stream.
		/// </summary>
		/// <param name="databusProperty"></param>
		/// <param name="stream"></param>
		void Serialize(object databusProperty, Stream stream);

		/// <summary>
		/// Deserializes a property from the given stream.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		object Deserialize(Stream stream);
	}
}