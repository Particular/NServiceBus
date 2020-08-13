namespace NServiceBus
{
    using System;
    using Extensibility;
    using Pipeline;

    abstract class BehaviorContext : ContextBag, IBehaviorContext
    {
        protected BehaviorContext(IBehaviorContext parentContext) : base(parentContext?.Extensions)
        {
        }

        public IServiceProvider Builder => Get<IServiceProvider>();

        public ContextBag Extensions => this;
    }
}