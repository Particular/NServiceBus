namespace NServiceBus.Sagas
{
    /// <summary>
    /// The property that this saga is correlated on.
    /// </summary>
    public class SagaCorrelationProperty
    {
        /// <summary>
        /// Initializes the correlation property.
        /// </summary>
        public SagaCorrelationProperty(string name, object value)
        {
            Guard.AgainstNullAndEmpty(nameof(name), name);
            Guard.AgainstNull(nameof(value), value);

            Name = name;
            Value = value;
        }

        /// <summary>
        /// The name of the property.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The property value.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Represents a saga with no correlated property.
        /// </summary>
        public static SagaCorrelationProperty None { get; } = null;
    }
}