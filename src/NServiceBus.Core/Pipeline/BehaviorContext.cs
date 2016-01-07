namespace NServiceBus
{
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;

    abstract class BehaviorContext : ContextBag, IBehaviorContext
    {
        protected BehaviorContext(IBehaviorContext parentContext) : base(parentContext?.Extensions)
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