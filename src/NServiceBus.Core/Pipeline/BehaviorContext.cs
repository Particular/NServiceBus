namespace NServiceBus
{
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;

    /// <summary>
    /// Provides base context for behavior context implementations.
    /// </summary>
    public abstract class BehaviorContext : ContextBag, IBehaviorContext
    {
        /// <summary>
        /// Creates a new instance of the behavior context.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        // ReSharper disable once SuggestBaseTypeForParameter
        protected BehaviorContext(IBehaviorContext parentContext) : base(parentContext?.Extensions)
        {
        }

        /// <summary>
        /// The current <see cref="IBuilder"/>.
        /// </summary>
        public IBuilder Builder
        {
            get
            {
                var rawBuilder = Get<IBuilder>();
                return rawBuilder;
            }
        }

        /// <summary>
        /// Gets the extensions.
        /// </summary>
        public ContextBag Extensions => this;
    }
}