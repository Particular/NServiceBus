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
        /// <param name="correlationProperty">The saga property used to correlate messages to this saga.</param>
        /// <param name="sagaMetadata">Metadata for the targeted saga.</param>
        /// <param name="extensions">A <see cref="ContextBag" /> which can be used to extend the current object.</param>
        public SagaIdGeneratorContext(SagaCorrelationProperty correlationProperty, SagaMetadata sagaMetadata, ContextBag extensions)
        {
            Guard.AgainstNull(nameof(sagaMetadata), sagaMetadata);
            Guard.AgainstNull(nameof(extensions), extensions);

            CorrelationProperty = correlationProperty;
            SagaMetadata = sagaMetadata;
            Extensions = extensions;
        }

        /// <summary>
        /// The saga property used to correlate messages to this saga.
        /// </summary>
        public SagaCorrelationProperty CorrelationProperty { get; }

        /// <summary>
        /// Metadata for the targeted saga.
        /// </summary>
        public SagaMetadata SagaMetadata { get; }

        /// <summary>
        /// A <see cref="ContextBag" /> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; }
    }
}