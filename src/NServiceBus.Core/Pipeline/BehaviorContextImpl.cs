namespace NServiceBus.Pipeline
{
    using Extensibility;
    using ObjectBuilder;

    /// <summary>
    /// Represents a context shared between pipeline behaviors.
    /// </summary>
    public interface BehaviorContext : IExtendable
    {
        /// <summary>
        /// The current <see cref="IBuilder"/>.
        /// </summary>
        IBuilder Builder { get; }
    }

    /// <summary>
    /// Base class for a pipeline behavior.
    /// </summary>
    public abstract class BehaviorContextImpl : ContextBag, BehaviorContext
    {
        /// <summary>
        /// Create an instance of <see cref="BehaviorContextImpl"/>.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        protected BehaviorContextImpl(BehaviorContext parentContext) : base(parentContext?.Extensions)
        {
        }

        /// <inheritdoc/>
        public IBuilder Builder
        {
            get
            {
                var rawBuilder = Get<IBuilder>();
                return rawBuilder;
            }
        }

        /// <inheritdoc/>
        ContextBag IExtendable.Extensions => this;
    }
}