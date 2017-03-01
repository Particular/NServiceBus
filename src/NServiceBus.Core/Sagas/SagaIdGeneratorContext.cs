namespace NServiceBus.Sagas
{
    using Extensibility;

    /// <summary>
    /// Context provided to the saga id generator.
    /// </summary>
    public class SagaIdGeneratorContext : ContextBag
    {

        /// <summary>
        /// Constructs a new context.
        /// </summary>
        /// <param name="correlationPropertyName">Name of property used to correlate messages to this saga. Can be <code>null</code> if a custom saga finder is used for the given message.</param>
        /// <param name="correlationPropertyValue">Value of the correlation property. Can be <code>null</code> if a custom saga finder is used for the given message.</param>
        /// <param name="sagaMetadata">Metadata for the targeted saga.</param>
        /// <param name="parentContext">Parent context.</param>
        public SagaIdGeneratorContext(string correlationPropertyName, object correlationPropertyValue, SagaMetadata sagaMetadata, ContextBag parentContext) : base(parentContext)
        {
            Guard.AgainstNull(nameof(sagaMetadata),sagaMetadata);
            Guard.AgainstNull(nameof(parentContext), parentContext);

            CorrelationPropertyName = correlationPropertyName;
            CorrelationPropertyValue = correlationPropertyValue;
            SagaMetadata = sagaMetadata;
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
    }
}