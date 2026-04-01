#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DelayedDelivery;
using Extensibility;
using Microsoft.Extensions.Time.Testing;
using Performance.TimeToBeReceived;
using Routing;
using Transport;

static class InlineExecutionTestHelper
{
    public static async Task<IMessageDispatcher> CreateDispatcher(InMemoryBroker broker, string[] receiveAddresses, CancellationToken cancellationToken = default)
    {
        var infrastructure = await CreateInfrastructure(broker, receiveAddresses, cancellationToken: cancellationToken);
        return infrastructure.Dispatcher;
    }

    public static Task<TransportInfrastructure> CreateInfrastructure(InMemoryBroker broker, string[] receiveAddresses, TransportTransactionMode transactionMode = TransportTransactionMode.SendsAtomicWithReceive, CancellationToken cancellationToken = default)
    {
        var transport = new InMemoryTransport(new InMemoryTransportOptions(broker) { InlineExecution = new() })
        {
            TransportTransactionMode = transactionMode
        };
        var receivers = receiveAddresses
            .Select((address, index) => new ReceiveSettings($"receiver-{index}", new QueueAddress(address), true, true, "error"))
            .ToArray();

        return transport.Initialize(
            new HostSettings("endpoint", string.Empty, new StartupDiagnosticEntries(), static (_, _, _) => { }, true),
            receivers,
            ["error"],
            cancellationToken);
    }

    public static TransportOperation CreateUnicast(string destination, DispatchConsistency consistency = DispatchConsistency.Isolated, TimeSpan? delay = null, TimeSpan? discardAfter = null, Dictionary<string, string>? headers = null)
    {
        var properties = new DispatchProperties();
        if (delay.HasValue)
        {
            properties.DelayDeliveryWith = new DelayDeliveryWith(delay.Value);
        }

        if (discardAfter.HasValue)
        {
            properties.DiscardIfNotReceivedBefore = new DiscardIfNotReceivedBefore(discardAfter.Value);
        }

        return new TransportOperation(CreateMessage(headers), new UnicastAddressTag(destination), properties, consistency);
    }

    public static TransportOperation CreateMulticast(Type messageType) => new(CreateMessage(), new MulticastAddressTag(messageType), [], DispatchConsistency.Isolated);

    public static OutgoingMessage CreateMessage(Dictionary<string, string>? headers = null) => new(Guid.NewGuid().ToString(), headers ?? [], new byte[] { 1 });

    public static BrokerEnvelope CreateReceivedEnvelope(string destination) => BrokerPayloadStore.Borrow(Guid.NewGuid().ToString(), [1], new Dictionary<string, string>(), destination, isPublished: false, sequenceNumber: 1);

    public static InlineExecutionScope CreateScope() => new(Guid.NewGuid());

    public static InMemoryReceiveTransaction CreateReceiveTransaction() => new();

    public static ReceivePipelineTransportTransactionMarker CreateReceivePipelineMarker() => ReceivePipelineTransportTransactionMarker.Instance;

    public static void AttachReceiveTransaction(TransportTransaction transaction, InMemoryReceiveTransaction receiveTransaction) =>
        transaction.Set<IInMemoryReceiveTransaction>(receiveTransaction);

    public static void AttachInlineScope(TransportTransaction transaction, InlineExecutionScope scope) =>
        transaction.Set(scope);

    public static IReadOnlyList<BrokerEnvelope> GetPendingEnvelopes(InMemoryReceiveTransaction receiveTransaction) =>
        receiveTransaction.GetPendingEnvelopesForTesting();

    public static InlineEnvelopeState? GetInlineState(BrokerEnvelope envelope) => envelope.InlineState;

    public static InlineExecutionScope? GetInlineScope(TransportTransaction transaction) =>
        transaction.TryGet<InlineExecutionScope>(out var scope)
            ? scope
            : null;

    public static InlineExecutionScope GetInlineScope(InlineEnvelopeState inlineState) => inlineState.Scope;

    public static Task GetCompletion(InlineExecutionScope scope, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return scope.Completion;
    }

    public static int GetDepth(InlineEnvelopeState inlineState) => inlineState.Depth;

    public static bool GetIsRootDispatch(InlineEnvelopeState inlineState) => inlineState.IsRootDispatch;

    public static InlineExecutionDispatchContext? GetInlineDispatchContext(ContextBag contextBag) =>
        contextBag.TryGet<InlineExecutionDispatchContext>(out var dispatchContext)
            ? dispatchContext
            : null;

    public static int GetPendingOperations(InlineExecutionScope scope) => scope.PendingOperations;

    public static int GetInlineDispatchDepth(InlineExecutionDispatchContext dispatchContext) => dispatchContext.Depth;

    public static InlineExecutionScope GetInlineDispatchScope(InlineExecutionDispatchContext dispatchContext) => dispatchContext.Scope;

    public static void SetRecoverabilityAction(ErrorContext errorContext, RecoverabilityAction action)
    {
        errorContext.Extensions.Set(action);
        errorContext.Extensions.Set(InlineRecoverabilityActionKey, action);
        errorContext.TransportTransaction.Set(action);
        errorContext.TransportTransaction.Set(InlineRecoverabilityActionKey, action);
    }

    public static Task DispatchRecoverabilityMessage(IMessageDispatcher dispatcher, ErrorContext errorContext, DispatchProperties dispatchProperties, CancellationToken cancellationToken = default) =>
        DispatchRecoverabilityMessage(dispatcher, errorContext, errorContext.ReceiveAddress, dispatchProperties, cancellationToken);

    public static Task DispatchRecoverabilityMessage(IMessageDispatcher dispatcher, ErrorContext errorContext, string destination, DispatchProperties dispatchProperties, CancellationToken cancellationToken = default)
    {
        var headers = new Dictionary<string, string>(errorContext.Message.Headers);
        var message = new OutgoingMessage(errorContext.Message.MessageId, headers, errorContext.Message.Body);
        var operation = new TransportOperation(message, new UnicastAddressTag(destination), dispatchProperties, DispatchConsistency.Isolated);
        return dispatcher.Dispatch(new TransportOperations(operation), errorContext.TransportTransaction, cancellationToken);
    }

    public static async Task<Exception> CatchException(Task task, CancellationToken cancellationToken = default)
    {
        try
        {
            await task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected the task to fault.");
    }

    public static FakeTimeProvider CreateFakeTimeProvider() => new(new DateTimeOffset(2099, 03, 28, 12, 0, 0, TimeSpan.Zero));

    const string InlineRecoverabilityActionKey = "NServiceBus.InMemory.InlineRecoverabilityAction";
}
