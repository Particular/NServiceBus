namespace NServiceBus
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using Extensibility;
    using Pipeline;

    class BehaviorContext : ContextBag, IBehaviorContext
    {
        protected BehaviorContext(IBehaviorContext parentContext) : this(parentContext, parentContext?.CancellationToken ?? default)
        { }

        [SuppressMessage("Code", "PCR0016:Methods should not have both CancellationToken parameters and parameters implementing ICancellableContext", Justification = "The cancellation tokens are linked together if required.")]
        public BehaviorContext(IBehaviorContext parentContext, CancellationToken cancellationToken = default) : base(parentContext?.Extensions)
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

        public CancellationToken CancellationToken { get; }
    }
}