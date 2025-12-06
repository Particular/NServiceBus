#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using Extensibility;
using Pipeline;

abstract class BehaviorContext(ContextBag? parentContext, CancellationToken cancellationToken = default)
    : ContextBag(parentContext), IBehaviorContext
{
    protected BehaviorContext(IBehaviorContext? parentContext) : this(parentContext?.Extensions, parentContext?.CancellationToken ?? CancellationToken.None)
    {
    }

    public IServiceProvider Builder => Get<IServiceProvider>();

    public ContextBag Extensions => this;

    public CancellationToken CancellationToken { get; } = cancellationToken;
}