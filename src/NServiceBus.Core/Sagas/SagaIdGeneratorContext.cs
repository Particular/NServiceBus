namespace NServiceBus.Sagas
{
    using Extensibility;

    /// <summary>
    /// Context provided to the saga id generator.
    /// </summary>
    public class SagaIdGeneratorContext : IExtendable
    {

        /// <summary>
        /// Constructs a new context.
        /// </summary>
        /// <param name="correlationPropertyName">Name of property used to correlate messages to this saga. Can be <code>null</code> if a custom saga finder is used for the given message.</param>
        /// <param name="correlationPropertyValue">Value of the correlation property. Can be <code>null</code> if a custom saga finder is used for the given message.</param>
        /// <param name="sagaMetadata">Metadata for the targeted saga.</param>
        /// <param name="extensions">A <see cref="ContextBag" /> which can be used to extend the current object.</param>
        public SagaIdGeneratorContext(string correlationPropertyName, object correlationPropertyValue, SagaMetadata sagaMetadata, ContextBag extensions)
        {
            Guard.AgainstNull(nameof(sagaMetadata), sagaMetadata);
            Guard.AgainstNull(nameof(extensions), extensions);

            CorrelationPropertyName = correlationPropertyName;
            CorrelationPropertyValue = correlationPropertyValue;
            SagaMetadata = sagaMetadata;
            Extensions = extensions;
        }

        /// <summary>
        /// Name of property used to correlate messages to this saga.
        /// </summary>
        public string CorrelationPropertyName { get; private set; }

        /// <summary>
        /// Value of the correlation property.
        /// </summary>
        public object CorrelationPropertyValue { get; private set; }

        /// <summary>
        /// Metadata for the targeted saga.
        /// </summary>
        public SagaMetadata SagaMetadata { get; private set; }

        /// <summary>
        /// A <see cref="ContextBag" /> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; }
    }
}