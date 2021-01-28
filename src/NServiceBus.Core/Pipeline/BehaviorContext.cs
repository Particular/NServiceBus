namespace NServiceBus
{
    using System;
    using System.Threading;
    using Extensibility;
    using Pipeline;

    abstract class BehaviorContext : ContextBag, IBehaviorContext
    {
        protected BehaviorContext(IBehaviorContext parentContext) : base(parentContext?.Extensions)
        {
        }

        public IServiceProvider Builder => Get<IServiceProvider>();

        public ContextBag Extensions => this;

        // TODO: Using CancellationToken.None until integrated with the pipeline
        public CancellationToken CancellationToken => CancellationToken.None;
    }
}