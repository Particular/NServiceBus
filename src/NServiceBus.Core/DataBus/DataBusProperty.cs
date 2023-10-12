namespace NServiceBus
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Default implementation for <see cref="IDataBusProperty" />.
    /// </summary>
    /// <typeparam name="T">Type of data to store in <see cref="IDataBusProperty" />.</typeparam>
    public class DataBusProperty<T> : IDataBusProperty where T : class
    {
        /// <summary>
        /// initializes a <see cref="DataBusProperty{T}" /> with no value set.
        /// </summary>
        public DataBusProperty() { }

        /// <summary>
        /// initializes a <see cref="DataBusProperty{T}" /> with the <paramref name="value" />.
        /// </summary>
        /// <param name="value">The value to initialize with.</param>
        public DataBusProperty(T value) => SetValue(value);

        /// <summary>
        /// The value.
        /// </summary>
#pragma warning disable IDE0032 // Use auto property - Value will be serialized into the message body if it is an auto property
        [JsonIgnore]
        public T Value => value;
#pragma warning restore IDE0032 // Use auto property

        /// <summary>
        /// The property <see cref="Type" />.
        /// </summary>
        [JsonIgnore]
        public Type Type { get; } = typeof(T);

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

#pragma warning disable IDE0032 // Use auto property - value will be serialized into the message body if it is an auto property
        T value;
#pragma warning restore IDE0032 // Use auto property
    }
}