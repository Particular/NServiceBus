namespace NServiceBus
{
    using Extensibility;
    using ObjectBuilder;
    using Pipeline;

    abstract class BehaviorContext : ContextBag, IBehaviorContext
    {
        protected BehaviorContext(IBehaviorContext parentContext) : base(parentContext?.Extensions)
        {
        }

        public IBuilder Builder => Get<IBuilder>();

        public ContextBag Extensions => this;
    }
}