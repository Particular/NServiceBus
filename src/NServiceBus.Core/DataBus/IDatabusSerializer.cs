namespace NServiceBus.DataBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    /// <summary>
	/// Interface used for serializing and deserializing of databus properties.
	/// </summary>
	public interface IDataBusSerializer
	{
		/// <summary>
		/// Serializes the property into the given stream.
		/// </summary>
		/// <param name="databusProperty">The property to serialize.</param>
		/// <param name="stream">The stream to which to write the property.</param>
		void Serialize(object databusProperty, Stream stream);

		/// <summary>
		/// Deserializes a property from the given stream.
		/// </summary>
		/// <param name="stream">The stream from which to read the property.</param>
		object Deserialize(Stream stream);

		/// <summary>
		/// Validates that the discovered databus properties can be serialized by this serializer.
		/// </summary>
		/// <param name="properties">The properties to be serialized.</param>
		/// <exception cref="Exception">One or more properties cannot be serialized by this serializer.</exception>
		void Validate(IEnumerable<PropertyInfo> properties);
	}
}