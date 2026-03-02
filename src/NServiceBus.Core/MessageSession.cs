#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Logging;

class MessageSession : IMessageSession
{
    internal MessageSession(object loggingSlot)
    {
        this.loggingSlot = loggingSlot;
        initializedTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [MemberNotNullWhen(true, nameof(serviceProvider), nameof(messageOperations), nameof(pipelineCache), nameof(endpointStoppingToken))]
    public bool Initialized => initializedTaskCompletionSource.Task.IsCompletedSuccessfully;

    [MemberNotNull(nameof(serviceProvider), nameof(messageOperations), nameof(pipelineCache), nameof(endpointStoppingToken))]
    internal void Initialize(
        IServiceProvider builder,
        MessageOperations operations,
        IPipelineCache cache,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(cache);

        if (Initialized)
        {
            return;
        }

        serviceProvider = builder;
        messageOperations = operations;
        pipelineCache = cache;
        endpointStoppingToken = cancellationToken;
        initializedTaskCompletionSource.SetResult();
    }

    [MemberNotNull(nameof(serviceProvider), nameof(messageOperations), nameof(pipelineCache), nameof(endpointStoppingToken))]
    async ValueTask WaitUntilInitialized(CancellationToken cancellationToken)
    {
        if (Initialized)
        {
            return;
        }

        // CS8774: the compiler cannot prove the fields are non-null after the await, but
        // Initialize() always assigns all three fields before calling SetResult(), so any
        // continuation that resumes here is guaranteed to see them as non-null.
#pragma warning disable CS8774 // Member must have a non-null value when exiting.
        await initializedTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
#pragma warning restore CS8774 // Member must have a non-null value when exiting.

    PipelineRootContext CreateContext(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(messageOperations);
        ArgumentNullException.ThrowIfNull(pipelineCache);

        return new PipelineRootContext(serviceProvider, messageOperations, pipelineCache, cancellationToken);
    }

    CancellationTokenSource CreateOperationLinkedTokenSource(CancellationToken cancellationToken)
    {
        try
        {
            return CancellationTokenSource.CreateLinkedTokenSource(endpointStoppingToken, cancellationToken);
        }
        catch (ObjectDisposedException ex)
        {
            throw new InvalidOperationException("Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.", ex);
        }
    }

    public async Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sendOptions);

        using var _ = LogManager.BeginSlotScope(loggingSlot!);
        await WaitUntilInitialized(cancellationToken).ConfigureAwait(false);
        using var linkedTokenSource = CreateOperationLinkedTokenSource(cancellationToken);
        await messageOperations.Send(CreateContext(linkedTokenSource.Token), message, sendOptions).ConfigureAwait(false);
    }

    public async Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageConstructor);
        ArgumentNullException.ThrowIfNull(sendOptions);

        using var _ = LogManager.BeginSlotScope(loggingSlot!);
        await WaitUntilInitialized(cancellationToken).ConfigureAwait(false);
        using var linkedTokenSource = CreateOperationLinkedTokenSource(cancellationToken);
        await messageOperations.Send(CreateContext(linkedTokenSource.Token), messageConstructor, sendOptions).ConfigureAwait(false);
    }

    public async Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishOptions);

        using var _ = LogManager.BeginSlotScope(loggingSlot!);
        await WaitUntilInitialized(cancellationToken).ConfigureAwait(false);
        using var linkedTokenSource = CreateOperationLinkedTokenSource(cancellationToken);
        await messageOperations.Publish(CreateContext(linkedTokenSource.Token), message, publishOptions).ConfigureAwait(false);
    }

    public async Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageConstructor);
        ArgumentNullException.ThrowIfNull(publishOptions);

        using var _ = LogManager.BeginSlotScope(loggingSlot!);
        await WaitUntilInitialized(cancellationToken).ConfigureAwait(false);
        using var linkedTokenSource = CreateOperationLinkedTokenSource(cancellationToken);
        await messageOperations.Publish(CreateContext(linkedTokenSource.Token), messageConstructor, publishOptions).ConfigureAwait(false);
    }

    public async Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(subscribeOptions);

        using var _ = LogManager.BeginSlotScope(loggingSlot!);
        await WaitUntilInitialized(cancellationToken).ConfigureAwait(false);
        using var linkedTokenSource = CreateOperationLinkedTokenSource(cancellationToken);
        await messageOperations.Subscribe(CreateContext(linkedTokenSource.Token), eventType, subscribeOptions).ConfigureAwait(false);
    }

    public async Task SubscribeAll(Type[] eventTypes, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
    {
        using var _ = LogManager.BeginSlotScope(loggingSlot!);
        await WaitUntilInitialized(cancellationToken).ConfigureAwait(false);
        // set a flag on the context so that subscribe implementations know which send API was used.
        subscribeOptions.Context.Set(SubscribeAllFlagKey, true);
        using var linkedTokenSource = CreateOperationLinkedTokenSource(cancellationToken);
        await messageOperations.Subscribe(CreateContext(linkedTokenSource.Token), eventTypes, subscribeOptions).ConfigureAwait(false);
    }

    public async Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(unsubscribeOptions);

        using var _ = LogManager.BeginSlotScope(loggingSlot!);
        await WaitUntilInitialized(cancellationToken).ConfigureAwait(false);
        using var linkedTokenSource = CreateOperationLinkedTokenSource(cancellationToken);
        await messageOperations.Unsubscribe(CreateContext(linkedTokenSource.Token), eventType, unsubscribeOptions).ConfigureAwait(false);
    }

    readonly object? loggingSlot;
    IServiceProvider? serviceProvider;
    MessageOperations? messageOperations;
    IPipelineCache? pipelineCache;
    CancellationToken endpointStoppingToken;
    readonly TaskCompletionSource initializedTaskCompletionSource;
    internal const string SubscribeAllFlagKey = "NServiceBus.SubscribeAllFlag";
}