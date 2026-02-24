#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

interface IEndpointStarter : IAsyncDisposable, IMessageSession
{
    ValueTask<IEndpointInstance> GetOrStart(CancellationToken cancellationToken = default);

    async Task IMessageSession.Send(object message, SendOptions sendOptions, CancellationToken cancellationToken)
    {
        var messageSession = await GetOrStart(cancellationToken).ConfigureAwait(false);
        await messageSession.Send(message, sendOptions, cancellationToken).ConfigureAwait(false);
    }

    async Task IMessageSession.Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken)
    {
        var messageSession = await GetOrStart(cancellationToken).ConfigureAwait(false);
        await messageSession.Send(messageConstructor, sendOptions, cancellationToken).ConfigureAwait(false);
    }

    async Task IMessageSession.Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken)
    {
        var messageSession = await GetOrStart(cancellationToken).ConfigureAwait(false);
        await messageSession.Publish(message, publishOptions, cancellationToken).ConfigureAwait(false);
    }

    async Task IMessageSession.Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken)
    {
        var messageSession = await GetOrStart(cancellationToken).ConfigureAwait(false);
        await messageSession.Publish(messageConstructor, publishOptions, cancellationToken).ConfigureAwait(false);
    }

    async Task IMessageSession.Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken)
    {
        var messageSession = await GetOrStart(cancellationToken).ConfigureAwait(false);
        await messageSession.Subscribe(eventType, subscribeOptions, cancellationToken).ConfigureAwait(false);
    }

    async Task IMessageSession.Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken)
    {
        var messageSession = await GetOrStart(cancellationToken).ConfigureAwait(false);
        await messageSession.Unsubscribe(eventType, unsubscribeOptions, cancellationToken).ConfigureAwait(false);
    }
}