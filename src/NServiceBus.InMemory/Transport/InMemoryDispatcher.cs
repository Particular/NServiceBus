namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transport;

class InMemoryDispatcher(
    InMemoryBroker broker,
    InlineExecutionSettings inlineExecutionSettings,
    HashSet<string> localReceiveAddresses,
    IReadOnlyDictionary<string, InlineExecutionRunner> inlineExecutionRunners,
    IReadOnlyDictionary<string, InMemoryMessagePump> pumpsByAddress) : IMessageDispatcher
{
    public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, CancellationToken cancellationToken = default)
    {
        var unicastTask = DispatchUnicast(outgoingMessages.UnicastTransportOperations, transaction, cancellationToken);
        var multicastTask = DispatchMulticast(outgoingMessages.MulticastTransportOperations, transaction, cancellationToken);

        return CombineTasks(cancellationToken, unicastTask, multicastTask);
    }

    Task DispatchMulticast(IEnumerable<MulticastTransportOperation> transportOperations, TransportTransaction transaction, CancellationToken cancellationToken)
    {
        List<Task> tasks = [];

        foreach (var transportOperation in transportOperations)
        {
            tasks.Add(DispatchMulticastOperation(transportOperation, transaction, cancellationToken));
        }

        return CombineTasks(tasks, cancellationToken);
    }

    HashSet<string> GetSubscribersForType(Type messageType)
    {
        HashSet<string> result = [];
        foreach (var type in GetPotentialEventTypes(messageType))
        {
            foreach (var subscriber in broker.GetSubscribers(type.FullName!))
            {
                result.Add(subscriber);
            }
        }
        return result;
    }

    static HashSet<Type> GetPotentialEventTypes(Type messageType)
    {
        HashSet<Type> allEventTypes = [];
        for (var current = messageType; current != null && !IsCoreMarkerInterface(current); current = current.BaseType)
        {
            allEventTypes.Add(current);
        }
        foreach (var iface in messageType.GetInterfaces())
        {
            if (!IsCoreMarkerInterface(iface))
            {
                allEventTypes.Add(iface);
            }
        }
        return allEventTypes;
    }

    static bool IsCoreMarkerInterface(Type type) =>
        type == typeof(IMessage) || type == typeof(IEvent) || type == typeof(ICommand);

    Task DispatchUnicast(IEnumerable<UnicastTransportOperation> operations, TransportTransaction transaction, CancellationToken cancellationToken)
    {
        List<Task> tasks = [];

        foreach (var operation in operations)
        {
            var task = DispatchUnicastOperation(operation, transaction, cancellationToken);
            tasks.Add(task);
        }

        return CombineTasks(tasks, cancellationToken);
    }

    async Task DispatchMulticastOperation(MulticastTransportOperation transportOperation, TransportTransaction transaction, CancellationToken cancellationToken)
    {
        var message = transportOperation.Message;
        var messageId = Guid.NewGuid().ToString();
        var sequenceNumber = broker.GetNextSequenceNumber();

        var subscribers = GetSubscribersForType(transportOperation.MessageType);

        foreach (var subscriber in subscribers)
        {
            var now = broker.GetCurrentTime();
            var deliverAt = GetDeliverAt(transportOperation.Properties, now);
            var discardAfter = GetDiscardAfter(transportOperation.Properties, now);

            var envelope = BrokerPayloadStore.Borrow(
                messageId,
                message.Body.Span,
                message.Headers,
                subscriber,
                isPublished: true,
                sequenceNumber,
                deliverAt,
                discardAfter);

            if (TryEnlistToReceiveTransaction(transaction, envelope, transportOperation.RequiredDispatchConsistency))
            {
                continue;
            }

            await broker.SimulateSendAsync(subscriber, cancellationToken).ConfigureAwait(false);

            if (deliverAt.HasValue)
            {
                broker.EnqueueDelayed(envelope, deliverAt.Value);
            }
            else
            {
                var queue = broker.GetOrCreateQueue(subscriber);
                await queue.Enqueue(envelope, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    Task DispatchUnicastOperation(UnicastTransportOperation operation, TransportTransaction transaction, CancellationToken cancellationToken)
    {
        var message = operation.Message;
        var messageId = Guid.NewGuid().ToString();
        var sequenceNumber = broker.GetNextSequenceNumber();
        var now = broker.GetCurrentTime();
        var deliverAt = GetDeliverAt(operation.Properties, now);
        var discardAfter = GetDiscardAfter(operation.Properties, now);

        var envelope = BrokerPayloadStore.Borrow(
            messageId,
            message.Body.Span,
            message.Headers,
            operation.Destination,
            isPublished: false,
            sequenceNumber,
            deliverAt,
            discardAfter);

        if (ShouldPreserveInlineScopeForDelayedRecoverability(transaction, deliverAt, out var preservedScope, out var preservedDispatchContext))
        {
            if (pumpsByAddress.TryGetValue(operation.Destination, out var pumpForDelayed))
            {
                pumpForDelayed.RegisterInlineScope(preservedScope!);
            }

            var preservedEnvelope = envelope with
            {
                InlineState = new InlineEnvelopeState(
                    preservedScope!,
                    preservedDispatchContext?.Depth ?? 0,
                    (preservedDispatchContext?.Depth ?? 0) == 0)
            };

            return DispatchRegularUnicast(operation, preservedEnvelope, deliverAt, cancellationToken);
        }

        if (ShouldInline(operation, transaction, deliverAt))
        {
            return DispatchInlineLocalUnicast(operation, envelope, transaction, deliverAt, cancellationToken);
        }

        if (TryEnlistToReceiveTransaction(transaction, envelope, operation.RequiredDispatchConsistency))
        {
            return Task.CompletedTask;
        }

        return DispatchRegularUnicast(operation, envelope, deliverAt, cancellationToken);
    }

    Task DispatchInlineLocalUnicast(UnicastTransportOperation operation, BrokerEnvelope envelope, TransportTransaction transaction, DateTimeOffset? deliverAt, CancellationToken cancellationToken)
    {
        var existingScope = TryGetInlineScope(transaction, out var scope);
        scope ??= new InlineExecutionScope(Guid.NewGuid());
        scope.RegisterDispatch();

        if (!existingScope)
        {
            if (pumpsByAddress.TryGetValue(operation.Destination, out var pump))
            {
                pump.RegisterInlineScope(scope);
            }
        }

        var inlineEnvelope = envelope with
        {
            InlineState = new InlineEnvelopeState(scope, existingScope ? 1 : 0, !existingScope)
        };
        var completion = scope.Completion;

        if (existingScope)
        {
            var runner = inlineExecutionRunners[operation.Destination];
            var processing = ProcessNestedInlineAsync(runner, inlineEnvelope, cancellationToken);

            ObserveReentrantProcessing(processing);

            return processing;
        }

        var preparation = DispatchRegularUnicast(operation, inlineEnvelope, deliverAt, cancellationToken);

        return preparation.IsCompletedSuccessfully ? completion : AwaitInlineDispatch(preparation, inlineEnvelope, scope, completion, cancellationToken);
    }

    static async Task AwaitInlineDispatch(Task preparation, BrokerEnvelope envelope, InlineExecutionScope scope, Task completion, CancellationToken cancellationToken)
    {
        try
        {
            await preparation.ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            envelope.Dispose();
            scope.MarkCanceled(ex);
            await completion.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            envelope.Dispose();
            scope.MarkTerminalFailure(ex);
            await completion.ConfigureAwait(false);
        }

        await completion.ConfigureAwait(false);
    }

    static void ObserveReentrantProcessing(Task processing)
    {
        if (processing.IsCompleted)
        {
            _ = processing.Exception;
            return;
        }

        _ = processing.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    static async Task ProcessNestedInlineAsync(InlineExecutionRunner runner, BrokerEnvelope envelope, CancellationToken cancellationToken)
    {
        await Task.Yield();
        await runner.Process(envelope, cancellationToken).ConfigureAwait(false);
    }

    async Task DispatchRegularUnicast(UnicastTransportOperation operation, BrokerEnvelope envelope, DateTimeOffset? deliverAt, CancellationToken cancellationToken)
    {
        await broker.SimulateSendAsync(operation.Destination, cancellationToken).ConfigureAwait(false);

        if (deliverAt.HasValue)
        {
            broker.EnqueueDelayed(envelope, deliverAt.Value);
            return;
        }

        var queue = broker.GetOrCreateQueue(operation.Destination);
        await queue.Enqueue(envelope, cancellationToken).ConfigureAwait(false);
    }

    bool ShouldInline(UnicastTransportOperation operation, TransportTransaction transaction, DateTimeOffset? deliverAt)
    {
        if (!inlineExecutionSettings.IsEnabled)
        {
            return false;
        }

        if (!localReceiveAddresses.Contains(operation.Destination))
        {
            return false;
        }

        var isInsideReceivePipeline = transaction.IsInsideReceivePipeline();

        var hasInlineScope = TryGetInlineScope(transaction, out _);

        if (!isInsideReceivePipeline)
        {
            return !hasInlineScope;
        }

        if (deliverAt.HasValue)
        {
            return false;
        }

        return hasInlineScope;
    }

    static bool TryGetInlineScope(TransportTransaction transaction, out InlineExecutionScope? scope) => transaction.TryGet(out scope);

    static bool ShouldPreserveInlineScopeForDelayedRecoverability(TransportTransaction transaction, DateTimeOffset? deliverAt, out InlineExecutionScope? scope, out InlineExecutionDispatchContext? dispatchContext)
    {
        dispatchContext = null;

        if (!deliverAt.HasValue || !transaction.TryGet<RecoverabilityAction>(out var action) || action is not DelayedRetry)
        {
            scope = null;
            return false;
        }

        if (!TryGetInlineScope(transaction, out scope) || scope == null)
        {
            return false;
        }

        _ = transaction.TryGet(out dispatchContext);

        return true;
    }

    static Task CombineTasks(CancellationToken cancellationToken, params Task[] tasks) => CombineTasks(tasks, cancellationToken);

    static Task CombineTasks(IEnumerable<Task> tasks, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var materialized = tasks.Where(static task => task != Task.CompletedTask).ToList();

        return materialized.Count switch
        {
            0 => Task.CompletedTask,
            1 => materialized[0],
            _ => Task.WhenAll(materialized)
        };
    }

    static DateTimeOffset? GetDeliverAt(DispatchProperties properties, DateTimeOffset now)
    {
        if (properties.DoNotDeliverBefore is not null)
        {
            return properties.DoNotDeliverBefore.At.ToUniversalTime();
        }

        return now + properties.DelayDeliveryWith?.Delay;
    }

    static DateTimeOffset? GetDiscardAfter(DispatchProperties properties, DateTimeOffset now)
    {
        var ttbr = properties.DiscardIfNotReceivedBefore;
        if (ttbr != null && ttbr.MaxTime < TimeSpan.MaxValue)
        {
            return now + ttbr.MaxTime;
        }
        return null;
    }

    static bool TryEnlistToReceiveTransaction(TransportTransaction transaction, BrokerEnvelope envelope, DispatchConsistency dispatchConsistency)
    {
        if (dispatchConsistency == DispatchConsistency.Isolated)
        {
            return false;
        }
        if (transaction.TryGet<IInMemoryReceiveTransaction>(out var receiveTransaction) && receiveTransaction != null)
        {
            receiveTransaction.Enlist(envelope);
            return true;
        }
        return false;
    }
}
