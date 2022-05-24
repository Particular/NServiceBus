namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;
    using System.Security;

    /// <summary>
    /// Default implementation for <see cref="IDataBusProperty" />.
    /// </summary>
    /// <typeparam name="T">Type of data to store in <see cref="IDataBusProperty" />.</typeparam>
    [Serializable]
    public class DataBusProperty<T> : IDataBusProperty, ISerializable where T : class
    {
        /// <summary>
        /// initializes a <see cref="DataBusProperty{T}" /> with no value set.
        /// </summary>
        public DataBusProperty() : this(null)
        {
            Type = typeof(T);
        }

        /// <summary>
        /// initializes a <see cref="DataBusProperty{T}" /> with the <paramref name="value" />.
        /// </summary>
        /// <param name="value">The value to initialize with.</param>
        public DataBusProperty(T value)
        {
            if (value != null)
            {
                Type = typeof(T);
            }

            SetValue(value);
        }

        /// <summary>
        /// For serialization purposes.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> to populate with data. </param>
        /// <param name="context">The destination (see <see cref="StreamingContext" />) for this serialization. </param>
        /// <exception cref="SecurityException">The caller does not have the required permission. </exception>
        protected DataBusProperty(SerializationInfo info, StreamingContext context)
        {
            Guard.AgainstNull(nameof(info), info);
            Key = info.GetString("Key");
            HasValue = info.GetBoolean("HasValue");
        }

        /// <summary>
        /// The value.
        /// </summary>
        public T Value => value;

        /// <summary>
        /// The property <see cref="Type" />.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The <see cref="IDataBusProperty" /> key.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// <code>true</code> if <see cref="IDataBusProperty" /> has a value.
        /// </summary>
        public bool HasValue { get; set; }

        /// <summary>
        /// Sets the value for <see cref="IDataBusProperty" />.
        /// </summary>
        /// <param name="valueToSet">The value to set.</param>
        public void SetValue(object valueToSet)
        {
            value = valueToSet as T;
            HasValue = value != null;
        }

        /// <summary>
        /// Gets the value of the <see cref="IDataBusProperty" />.
        /// </summary>
        /// <returns>The value.</returns>
        public object GetValue()
        {
            return Value;
        }


        /// <summary>
        /// Populates a <see cref="SerializationInfo" /> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo" /> to populate with data. </param>
        /// <param name="context">The destination (see <see cref="StreamingContext" />) for this serialization. </param>
        /// <exception cref="T:System.Security.SecurityException">The caller does not have the required permission. </exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Guard.AgainstNull(nameof(info), info);
            info.AddValue("Key", Key);
            info.AddValue("HasValue", HasValue);
        }

        T value;
    }
}