namespace NServiceBus.Pipeline
{
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;

    /// <summary>
    /// Base class for a pipeline behavior.
    /// </summary>
    abstract class BehaviorContextImpl : ContextBag, BehaviorContext
    {
        protected BehaviorContextImpl(BehaviorContext parentContext) : base(parentContext?.Extensions)
        {
        }

        public IBuilder Builder
        {
            get
            {
                var rawBuilder = Get<IBuilder>();
                return rawBuilder;
            }
        }

        public ContextBag Extensions => this;
    }
}