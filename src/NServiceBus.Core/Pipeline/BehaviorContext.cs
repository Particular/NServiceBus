namespace NServiceBus
{
    using System;
    using System.Threading;
    using Extensibility;
    using Pipeline;

    abstract class BehaviorContext : ContextBag, IBehaviorContext
    {
        protected BehaviorContext(IBehaviorContext parentContext, CancellationToken cancellationToken) : base(parentContext?.Extensions)
        {
            if (parentContext != null)
            {
                if (cancellationToken == default || cancellationToken == parentContext.CancellationToken)
                {
                    CancellationToken = parentContext.CancellationToken;
                }
                else
                {
                    var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(parentContext.CancellationToken, cancellationToken);
                    CancellationToken = linkedSource.Token;
                }
            }
            else
            {
                CancellationToken = cancellationToken;
            }
        }

        public IServiceProvider Builder => Get<IServiceProvider>();

        public ContextBag Extensions => this;

        public CancellationToken CancellationToken { get; private set; }
    }
}