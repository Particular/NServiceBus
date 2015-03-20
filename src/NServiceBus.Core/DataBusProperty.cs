namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;
    using System.Security;

    /// <summary>
    /// Default implementation for <see cref="IDataBusProperty"/>.
    /// </summary>
    /// <typeparam name="T">Type of data to store in <see cref="IDataBusProperty"/>.</typeparam>
    [Serializable]
	public class DataBusProperty<T> : IDataBusProperty, ISerializable where T : class
    {
        T value;
    	
        /// <summary>
        /// initializes a <see cref="DataBusProperty{T}"/> with the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value to initialise with.</param>
        public DataBusProperty(T value)
        {
            SetValue(value);
        }

        /// <summary>
        /// For serialization purposes.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data. </param><param name="context">The destination (see <see cref="StreamingContext"/>) for this serialization. </param><exception cref="SecurityException">The caller does not have the required permission. </exception>
        protected DataBusProperty(SerializationInfo info, StreamingContext context)
        {
            Guard.AgainstNull(info, "info");
            Guard.AgainstNull(context, "context");
            Key = info.GetString("Key");
			HasValue = info.GetBoolean("HasValue");
        }

        /// <summary>
        /// The <see cref="IDataBusProperty"/> key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// <code>true</code> if <see cref="IDataBusProperty"/> has a value.
        /// </summary>
        public bool HasValue { get; set; }

    	/// <summary>
    	/// The value.
    	/// </summary>
    	public T Value
        {
            get
            {
                return value;
            }
        }


        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data. </param><param name="context">The destination (see <see cref="StreamingContext"/>) for this serialization. </param><exception cref="T:System.Security.SecurityException">The caller does not have the required permission. </exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Guard.AgainstNull(info, "info");
            Guard.AgainstNull(context, "context");
            info.AddValue("Key", Key);
			info.AddValue("HasValue", HasValue);
		}

        /// <summary>
        /// Sets the value for <see cref="IDataBusProperty"/>.
        /// </summary>
        /// <param name="valueToSet">The value to set.</param>
        public void SetValue(object valueToSet)
		{
			value = valueToSet as T;

			if (value != null)
				HasValue = true;
		}

        /// <summary>
        /// Gets the value of the <see cref="IDataBusProperty"/>.
        /// </summary>
        /// <returns>The value</returns>
        public object GetValue()
		{
			return Value;
		}

    }

    /// <summary>
    /// The contract to implement a <see cref="IDataBusProperty"/>.
    /// </summary>
	public interface IDataBusProperty
	{
        /// <summary>
        /// The <see cref="IDataBusProperty"/> key.
        /// </summary>
		string Key { get; set; }

        /// <summary>
        /// Gets the value of the <see cref="IDataBusProperty"/>.
        /// </summary>
        /// <returns>The value</returns>
		object GetValue();

        /// <summary>
        /// Sets the value for <see cref="IDataBusProperty"/>.
        /// </summary>
        /// <param name="value">The value to set.</param>
		void SetValue(object value);

        /// <summary>
        /// <code>true</code> if <see cref="IDataBusProperty"/> has a value.
        /// </summary>
		bool HasValue { get; set; }
	}
}