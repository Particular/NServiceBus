namespace NServiceBus.OutgoingPipeline
{
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;

    /// <summary>
    /// The abstract base context for everything inside the outgoing pipeline.
    /// </summary>
    public abstract class OutgoingContext : BehaviorContext
    {
        /// <summary>
        /// Initializes a new <see cref="OutgoingContext"/>.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        protected OutgoingContext(BehaviorContext parentContext)
            : base(parentContext)
        {
        }

        /// <inheritdoc/>
        public ContextBag Extensions => this;
    }
}