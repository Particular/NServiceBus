#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Particular.Obsoletes;

#pragma warning disable CS0618 // The self-hosting API remains supported until its scheduled removal.
sealed class SelfHostedEndpointInstance(RunningEndpointInstance endpoint) : IEndpointInstance, IAsyncDisposable
#pragma warning restore CS0618
{
    // Receive infrastructure roots the running instance. Keep this caller-owned handle outside that graph.
    ~SelfHostedEndpointInstance() => ThreadPool.UnsafeQueueUserWorkItem(
        static instance => _ = instance.StopAfterAbandonment(new CancellationToken(true)), endpoint, preferLocal: false);

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        try
        {
            await endpoint.Stop(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DisposeAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await endpoint.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            GC.SuppressFinalize(this);
        }
    }

    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7906",
        ReplacementTypeOrMember = "Send<T>(T, SendOptions, CancellationToken) or Send(object, Type, SendOptions, CancellationToken)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic or explicit Type overload instead.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    public async Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sendOptions);

        await endpoint.Send(message, sendOptions, cancellationToken).ConfigureAwait(false);
        GC.KeepAlive(this);
    }

    [OverloadResolutionPriority(-1)]
    public async Task Send<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sendOptions);

        await endpoint.Send<T>(message, sendOptions, cancellationToken).ConfigureAwait(false);
        GC.KeepAlive(this);
    }

    public async Task Send(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(sendOptions);

        await endpoint.Send(message, messageType, sendOptions, cancellationToken).ConfigureAwait(false);
        GC.KeepAlive(this);
    }

    public async Task Send<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageConstructor);
        ArgumentNullException.ThrowIfNull(sendOptions);

        await endpoint.Send(messageConstructor, sendOptions, cancellationToken).ConfigureAwait(false);
        GC.KeepAlive(this);
    }

    [PreObsolete("https://github.com/Particular/NServiceBus/issues/7906",
        ReplacementTypeOrMember = "Publish<T>(T, PublishOptions, CancellationToken) or Publish(object, Type, PublishOptions, CancellationToken)",
        Note = "The object-only overload uses message.GetType() at runtime which is not trimming safe. Use the generic or explicit Type overload instead.")]
    [RequiresUnreferencedCode(MessageOperations.RuntimeTypeRoutingTrimmingMessage)]
    public async Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishOptions);

        await endpoint.Publish(message, publishOptions, cancellationToken).ConfigureAwait(false);
        GC.KeepAlive(this);
    }

    [OverloadResolutionPriority(-1)]
    public async Task Publish<[DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] T>(T message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishOptions);

        await endpoint.Publish<T>(message, publishOptions, cancellationToken).ConfigureAwait(false);
        GC.KeepAlive(this);
    }

    public async Task Publish(object message, [DynamicallyAccessedMembers(DynamicMemberTypeAccess.Message)] Type messageType, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        ArgumentNullException.ThrowIfNull(publishOptions);

        await endpoint.Publish(message, messageType, publishOptions, cancellationToken).ConfigureAwait(false);
        GC.KeepAlive(this);
    }

    public async Task Publish<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageConstructor);
        ArgumentNullException.ThrowIfNull(publishOptions);

        await endpoint.Publish(messageConstructor, publishOptions, cancellationToken).ConfigureAwait(false);
        GC.KeepAlive(this);
    }

    public async Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(subscribeOptions);

        await endpoint.Subscribe(eventType, subscribeOptions, cancellationToken).ConfigureAwait(false);
        GC.KeepAlive(this);
    }

    public async Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(unsubscribeOptions);

        await endpoint.Unsubscribe(eventType, unsubscribeOptions, cancellationToken).ConfigureAwait(false);
        GC.KeepAlive(this);
    }
}
