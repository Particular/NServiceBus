namespace NServiceBus
{
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;

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