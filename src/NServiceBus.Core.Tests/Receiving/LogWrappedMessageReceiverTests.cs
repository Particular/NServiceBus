#nullable enable

namespace NServiceBus.Core.Tests.Receiving;

using System;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using NServiceBus.Logging;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class LogWrappedMessageReceiverTests
{
    [Test]
    public async Task Should_scope_on_message_callback()
    {
        var slot = new EndpointLogSlot("Sales", endpointIdentifier: "blue");
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, slot);

        LogScopeState? scope = null;

        await wrapped.Initialize(new PushRuntimeSettings(1), (messageContext, ct) =>
        {
            _ = messageContext;
            _ = ct;
            Assert.That(LogManager.TryGetCurrentEndpointScopeState(out var currentScope), Is.True);
            scope = currentScope;
            return Task.CompletedTask;
        }, (errorContext, ct) =>
        {
            _ = errorContext;
            _ = ct;
            return Task.FromResult(ErrorHandleResult.Handled);
        });

        await receiver.InvokeMessage(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(scope, Is.Not.Null);
            Assert.That(scope![0].Key, Is.EqualTo("Endpoint"));
            Assert.That(scope[0].Value, Is.EqualTo("Sales"));
            Assert.That(scope[1].Key, Is.EqualTo("EndpointIdentifier"));
            Assert.That(scope[1].Value, Is.EqualTo("blue"));
        }
    }

    [Test]
    public async Task Should_scope_on_error_callback()
    {
        var slot = new EndpointLogSlot("Billing", endpointIdentifier: null);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, slot);

        LogScopeState? scope = null;

        await wrapped.Initialize(new PushRuntimeSettings(1), (messageContext, ct) =>
        {
            _ = messageContext;
            _ = ct;
            return Task.CompletedTask;
        }, (errorContext, ct) =>
        {
            _ = errorContext;
            _ = ct;
            Assert.That(LogManager.TryGetCurrentEndpointScopeState(out var currentScope), Is.True);
            scope = currentScope;
            return Task.FromResult(ErrorHandleResult.Handled);
        });

        var result = await receiver.InvokeError(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(ErrorHandleResult.Handled));
            Assert.That(scope, Is.Not.Null);
            Assert.That(scope!.Count, Is.EqualTo(1));
            Assert.That(scope[0].Key, Is.EqualTo("Endpoint"));
            Assert.That(scope[0].Value, Is.EqualTo("Billing"));
        }
    }

    sealed class TestReceiver : IMessageReceiver
    {
        public ISubscriptionManager Subscriptions { get; } = null!;
        public string Id => "receiver";
        public string ReceiveAddress => "queue";

        public Task ChangeConcurrency(PushRuntimeSettings limitations, CancellationToken cancellationToken = default)
        {
            _ = limitations;
            _ = cancellationToken;
            return Task.CompletedTask;
        }

        public Task StartReceive(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.CompletedTask;
        }

        public Task StopReceive(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.CompletedTask;
        }

        public Task Initialize(PushRuntimeSettings limitations, OnMessage onMessage, OnError onError, CancellationToken cancellationToken = default)
        {
            _ = limitations;
            _ = cancellationToken;
            onMessageCallback = onMessage;
            onErrorCallback = onError;
            return Task.CompletedTask;
        }

        public Task InvokeMessage(CancellationToken cancellationToken = default) => onMessageCallback(CreateMessageContext(), cancellationToken);

        public Task<ErrorHandleResult> InvokeError(CancellationToken cancellationToken = default) => onErrorCallback(CreateErrorContext(), cancellationToken);

        OnMessage onMessageCallback = null!;
        OnError onErrorCallback = null!;

        static MessageContext CreateMessageContext() =>
            new(
                Guid.NewGuid().ToString(),
                [],
                Array.Empty<byte>(),
                new TransportTransaction(),
                "receiver",
                new ContextBag());

        static ErrorContext CreateErrorContext() =>
            new(
                new Exception("boom"),
                [],
                Guid.NewGuid().ToString(),
                Array.Empty<byte>(),
                new TransportTransaction(),
                immediateProcessingFailures: 1,
                receiveAddress: "queue",
                new ContextBag());
    }
}