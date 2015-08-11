namespace NServiceBus.Saga
{
    using NServiceBus.Extensibility;

    /// <summary>
    /// Contains details about the to be persisted saga.
    /// </summary>
    public class SagaPersistenceOptions
    {
        /// <summary>
        /// Creates a new instance of the SagaPersistenceOptions class.
        /// </summary>
        /// <param name="sagaMetadata">The saga metadata representing the to be persisted saga.</param>
        /// <param name="context">The context.</param>
        public SagaPersistenceOptions(SagaMetadata sagaMetadata, ContextBag context = null)
        {
            Metadata = sagaMetadata;
            Context = context;

            if (context == null)
            {
                Context = new ContextBag();
            }
        }

        /// <summary>
        /// The saga metadata representing the to be persisted saga.
        /// </summary>
        public SagaMetadata Metadata { get; private set; }

        /// <summary>
        /// Access to the behavior context.
        /// </summary>
        public ContextBag Context { get; private set; }
    }
}