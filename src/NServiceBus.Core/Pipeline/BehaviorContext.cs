namespace NServiceBus
{
    using System;
    using System.Threading;
    using Extensibility;
    using Pipeline;

    abstract class BehaviorContext : ContextBag, IBehaviorContext
    {
        protected BehaviorContext(IBehaviorContext parentContext) : this(parentContext?.Extensions, parentContext?.CancellationToken ?? default)
        {
        }

        public BehaviorContext(ContextBag parentContext, CancellationToken cancellationToken = default) : base(parentContext)
        {
            CancellationToken = cancellationToken;
        }

        public IServiceProvider Builder => Get<IServiceProvider>();

        public ContextBag Extensions => this;

        public CancellationToken CancellationToken { get; }
    }
}