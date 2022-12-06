namespace NServiceBus
{
    using System;
    using System.Threading;
    using Extensibility;
    using Pipeline;

    class BehaviorContext : ContextBag, IBehaviorContext
    {
        //TODO can parent ever be null here?
        protected BehaviorContext(IBehaviorContext parentContext) : this(parentContext.Extensions, parentContext.CancellationToken)
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