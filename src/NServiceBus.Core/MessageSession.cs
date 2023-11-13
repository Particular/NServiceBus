namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

class MessageSession : IMessageSession
{
    public MessageSession(IServiceProvider builder, MessageOperations messageOperations, PipelineCache pipelineCache, CancellationToken cancellationToken)
    {
        this.builder = builder;
        this.messageOperations = messageOperations;
        this.pipelineCache = pipelineCache;
        endpointStoppingToken = cancellationToken;
    }

    PipelineRootContext CreateContext(CancellationToken cancellationToken) => new(builder, messageOperations, pipelineCache, cancellationToken);

    public async Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sendOptions);
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(endpointStoppingToken, cancellationToken);
        await messageOperations.Send(CreateContext(linkedTokenSource.Token), message, sendOptions).ConfigureAwait(false);
    }

    public async Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageConstructor);
        ArgumentNullException.ThrowIfNull(sendOptions);
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(endpointStoppingToken, cancellationToken);
        await messageOperations.Send(CreateContext(linkedTokenSource.Token), messageConstructor, sendOptions).ConfigureAwait(false);
    }

    public async Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishOptions);
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(endpointStoppingToken, cancellationToken);
        await messageOperations.Publish(CreateContext(linkedTokenSource.Token), message, publishOptions).ConfigureAwait(false);
    }

    public async Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageConstructor);
        ArgumentNullException.ThrowIfNull(publishOptions);
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(endpointStoppingToken, cancellationToken);
        await messageOperations.Publish(CreateContext(linkedTokenSource.Token), messageConstructor, publishOptions).ConfigureAwait(false);
    }

    public async Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(subscribeOptions);
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(endpointStoppingToken, cancellationToken);
        await messageOperations.Subscribe(CreateContext(linkedTokenSource.Token), eventType, subscribeOptions).ConfigureAwait(false);
    }

    public async Task SubscribeAll(Type[] eventTypes, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
    {
        // set a flag on the context so that subscribe implementations know which send API was used.
        subscribeOptions.Context.Set(SubscribeAllFlagKey, true);
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(endpointStoppingToken, cancellationToken);
        await messageOperations.Subscribe(CreateContext(linkedTokenSource.Token), eventTypes, subscribeOptions).ConfigureAwait(false);
    }

    public async Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(unsubscribeOptions);
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(endpointStoppingToken, cancellationToken);
        await messageOperations.Unsubscribe(CreateContext(linkedTokenSource.Token), eventType, unsubscribeOptions).ConfigureAwait(false);
    }

    readonly IServiceProvider builder;
    readonly MessageOperations messageOperations;
    readonly PipelineCache pipelineCache;
    readonly CancellationToken endpointStoppingToken;

    internal const string SubscribeAllFlagKey = "NServiceBus.SubscribeAllFlag";
}