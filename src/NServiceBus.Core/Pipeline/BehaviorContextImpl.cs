namespace NServiceBus.Pipeline
{
    using Extensibility;
    using ObjectBuilder;

    /// <summary>
    /// 
    /// </summary>
    public interface BehaviorContext : ContextBag
    {
        /// <summary>
        /// The current <see cref="IBuilder"/>.
        /// </summary>
        IBuilder Builder { get; }
    }

    /// <summary>
    /// Base class for a pipeline behavior.
    /// </summary>
    public abstract class BehaviorContextImpl : ContextBagImpl, BehaviorContext
    {
        /// <summary>
        /// Create an instance of <see cref="BehaviorContextImpl"/>.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        protected BehaviorContextImpl(BehaviorContext parentContext) : base(parentContext)
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
    }
}