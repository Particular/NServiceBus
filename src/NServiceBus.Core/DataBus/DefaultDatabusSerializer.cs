using System;
using System.Collections.Generic;
using System.Reflection;

namespace NServiceBus.DataBus
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;

    public class DefaultDataBusSerializer : IDataBusSerializer
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

		/// <summary>
		/// Validates that the discovered databus properties can be serialized by this serializer.
		/// </summary>
		/// <param name="properties">The properties to be serialized.</param>
		/// <exception cref="Exception">One or more properties cannot be serialized by this serializer.</exception>
		public void Validate(IEnumerable<PropertyInfo> properties)
		{
			if (System.Diagnostics.Debugger.IsAttached)
			{
				foreach (PropertyInfo property in properties)
				{
					if (!property.PropertyType.IsSerializable)
					{
						throw new InvalidOperationException(
							String.Format(
								@"The property type for '{0}' is not serializable. 
In order to use the databus feature for transporting the data stored in the property, types defined in the call '.DefiningDataBusPropertiesAs()' need to be serializable. 
To fix this, please mark the property type '{0}' as serializable, see http://msdn.microsoft.com/en-us/library/system.runtime.serialization.iserializable.aspx on how to do this.",
								String.Format("{0}.{1}", property.DeclaringType.FullName, property.Name)
							)
						);
					}
				}
			}
		}
    }
}