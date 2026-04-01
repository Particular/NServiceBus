#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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

    public static object CreateScope() => Activator.CreateInstance(InlineExecutionScopeType, Guid.NewGuid())!;

    public static object CreateReceiveTransaction() => Activator.CreateInstance(InMemoryReceiveTransactionType)!;

    public static object CreateReceivePipelineMarker() => ReceivePipelineTransportTransactionMarkerType.GetField("Instance", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    public static IReadOnlyList<BrokerEnvelope> GetPendingEnvelopes(object receiveTransaction) =>
        (IReadOnlyList<BrokerEnvelope>)InMemoryReceiveTransactionType
            .GetField("pendingEnvelopes", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(receiveTransaction)!;

    public static object? GetInlineState(BrokerEnvelope envelope) => InlineStateProperty.GetValue(envelope);

    public static object? GetInlineScope(TransportTransaction transaction) =>
        transaction.TryGet(InlineExecutionScopeType.FullName!, out object? scope)
            ? scope
            : null;

    public static object GetScope(object inlineState) => InlineStateType.GetProperty("Scope", BindingFlags.Instance | BindingFlags.Public)!.GetValue(inlineState)!;

    public static Task GetCompletion(object scope, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        return (Task)InlineExecutionScopeType.GetProperty("Completion", BindingFlags.Instance | BindingFlags.Public)!.GetValue(scope)!;
    }

    public static int GetDepth(object inlineState) => (int)InlineStateType.GetProperty("Depth", BindingFlags.Instance | BindingFlags.Public)!.GetValue(inlineState)!;

    public static bool GetIsRootDispatch(object inlineState) => (bool)InlineStateType.GetProperty("IsRootDispatch", BindingFlags.Instance | BindingFlags.Public)!.GetValue(inlineState)!;

    public static object? GetInlineDispatchContext(ContextBag contextBag) =>
        contextBag.TryGet(InlineExecutionDispatchContextType.FullName!, out object? dispatchContext)
            ? dispatchContext
            : null;

    public static int GetPendingOperations(object scope) => (int)InlineExecutionScopeType.GetProperty("PendingOperations", BindingFlags.Instance | BindingFlags.Public)!.GetValue(scope)!;

    public static int GetInlineDispatchDepth(object dispatchContext) => (int)InlineExecutionDispatchContextType.GetProperty("Depth", BindingFlags.Instance | BindingFlags.Public)!.GetValue(dispatchContext)!;

    public static object GetInlineDispatchScope(object dispatchContext) => InlineExecutionDispatchContextType.GetProperty("Scope", BindingFlags.Instance | BindingFlags.Public)!.GetValue(dispatchContext)!;

    public static object GetInlineScope(object inlineState) => InlineEnvelopeStateType.GetProperty("Scope", BindingFlags.Instance | BindingFlags.Public)!.GetValue(inlineState)!;

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

    static readonly Assembly InMemoryAssembly = typeof(InMemoryTransport).Assembly;
    static readonly Type InlineExecutionScopeType = InMemoryAssembly.GetType("NServiceBus.InlineExecutionScope", throwOnError: true)!;
    static readonly Type InlineExecutionDispatchContextType = InMemoryAssembly.GetType("NServiceBus.InlineExecutionDispatchContext", throwOnError: true)!;
    static readonly Type InlineStateType = InMemoryAssembly.GetType("NServiceBus.InlineEnvelopeState", throwOnError: true)!;
    static readonly Type InlineEnvelopeStateType = InMemoryAssembly.GetType("NServiceBus.InlineEnvelopeState", throwOnError: true)!;
    static readonly Type InMemoryReceiveTransactionType = InMemoryAssembly.GetType("NServiceBus.InMemoryReceiveTransaction", throwOnError: true)!;
    static readonly Type ReceivePipelineTransportTransactionMarkerType = InMemoryAssembly.GetType("NServiceBus.ReceivePipelineTransportTransactionMarker", throwOnError: true)!;
    static readonly PropertyInfo InlineStateProperty = typeof(BrokerEnvelope).GetProperty("InlineState", BindingFlags.Instance | BindingFlags.NonPublic)!;
    const string InlineRecoverabilityActionKey = "NServiceBus.InMemory.InlineRecoverabilityAction";
}
