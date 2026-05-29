#nullable enable

namespace NServiceBus.Core.Tests.Receiving;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NServiceBus.Logging;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class LogWrappedMessageReceiverTests
{
    [Test]
    public async Task Should_scope_on_message_callback()
    {
        var slot = new TestLogSlot();
        var fakeFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new MicrosoftLoggerFactoryAdapter(fakeFactory);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, slot, slotFactory, manageSlotLifecycle: false);

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

        Assert.That(scope, Is.SameAs(slot.ScopeState));
    }

    [Test]
    public async Task Should_scope_on_error_callback()
    {
        var slot = new TestLogSlot();
        var fakeFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new MicrosoftLoggerFactoryAdapter(fakeFactory);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, slot, slotFactory, manageSlotLifecycle: false);

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

        await receiver.InvokeError(CancellationToken.None);

        Assert.That(scope, Is.SameAs(slot.ScopeState));
    }

    [Test]
    public async Task Should_register_and_unregister_slot_when_managing_lifecycle()
    {
        var slot = new TestLogSlot();
        var fakeFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new MicrosoftLoggerFactoryAdapter(fakeFactory);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, slot, slotFactory, manageSlotLifecycle: true);

        await wrapped.Initialize(new PushRuntimeSettings(1), (messageContext, ct) =>
        {
            _ = messageContext;
            _ = ct;
            return Task.CompletedTask;
        }, (errorContext, ct) =>
        {
            _ = errorContext;
            _ = ct;
            return Task.FromResult(ErrorHandleResult.Handled);
        });

        Assert.That(slot.IsFactoryRegistered, Is.True, "Slot factory should be registered after Initialize");

        await wrapped.StopReceive(CancellationToken.None);

        Assert.That(slot.IsFactoryRegistered, Is.False, "Slot factory should be unregistered after StopReceive");
    }

    [Test]
    public async Task Should_not_register_or_unregister_slot_when_not_managing_lifecycle()
    {
        var slot = new TestLogSlot();
        var fakeFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new MicrosoftLoggerFactoryAdapter(fakeFactory);
        LogManager.RegisterSlotFactory(slot, slotFactory);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, slot, slotFactory, manageSlotLifecycle: false);

        await wrapped.Initialize(new PushRuntimeSettings(1), (messageContext, ct) =>
        {
            _ = messageContext;
            _ = ct;
            return Task.CompletedTask;
        }, (errorContext, ct) =>
        {
            _ = errorContext;
            _ = ct;
            return Task.FromResult(ErrorHandleResult.Handled);
        });

        Assert.That(slot.IsFactoryRegistered, Is.True, "Slot should remain registered after Initialize");

        await wrapped.StopReceive(CancellationToken.None);

        Assert.That(slot.IsFactoryRegistered, Is.True, "Slot should remain registered after StopReceive");

        LogManager.UnregisterSlot(slot);
    }

    [Test]
    public async Task Should_route_logs_through_registered_slot_factory()
    {
        var slot = new TestLogSlot();
        var fakeFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new MicrosoftLoggerFactoryAdapter(fakeFactory);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, slot, slotFactory, manageSlotLifecycle: true);

        var loggerName = $"{nameof(LogWrappedMessageReceiverTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        await wrapped.Initialize(new PushRuntimeSettings(1), (messageContext, ct) =>
        {
            logger.Info("slot-processed-log");
            return Task.CompletedTask;
        }, (errorContext, ct) =>
        {
            _ = errorContext;
            _ = ct;
            return Task.FromResult(ErrorHandleResult.Handled);
        });

        await receiver.InvokeMessage(CancellationToken.None);

        Assert.That(fakeFactory.CapturedMessages, Does.Contain("slot-processed-log"),
            "Logs written during slot-scoped message processing must reach the registered slot factory.");

        await wrapped.StopReceive(CancellationToken.None);
    }

    sealed class TestLogSlot : LogSlot
    {
        public override LogScopeState ScopeState { get; } = new TestLogScopeState();

        sealed class TestLogScopeState : LogScopeState
        {
            public override KeyValuePair<string, object?> this[int index] => throw new IndexOutOfRangeException();
            public override int Count => 0;
            public override IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => Enumerable.Empty<KeyValuePair<string, object?>>().GetEnumerator();
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

    sealed class FakeLoggerLoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory
    {
        readonly FakeLoggerProvider provider = new();

        public List<string> CapturedMessages => [.. provider.Collector.GetSnapshot().Select(r => r.Message)];

        public void AddProvider(ILoggerProvider loggerProvider) => throw new NotSupportedException();

        public ILogger CreateLogger(string categoryName) => provider.CreateLogger(categoryName);

        public void Dispose() => provider.Dispose();
    }
}