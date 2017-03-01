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
        public SagaIdGeneratorContext(string correlationPropertyName, object correlationPropertyValue, SagaMetadata sagaMetadata, ContextBag parentBag) : base(parentBag)
        {
            Guard.AgainstNull(nameof(sagaMetadata),sagaMetadata);
            Guard.AgainstNull(nameof(parentBag), parentBag);

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