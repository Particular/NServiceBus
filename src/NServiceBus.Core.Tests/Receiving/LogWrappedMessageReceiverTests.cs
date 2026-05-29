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
        var slot = new EndpointLogSlot("Sales", endpointIdentifier: "blue");
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

    [Test]
    public async Task Should_register_slot_factory_for_satellite_on_initialize()
    {
        var endpointSlot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var satelliteSlot = new EndpointSatelliteLogSlot(endpointSlot, "TimeoutMigration");
        var fakeFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new MicrosoftLoggerFactoryAdapter(fakeFactory);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, satelliteSlot, slotFactory, manageSlotLifecycle: true);

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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(satelliteSlot.IsFactoryRegistered, Is.True, "Satellite slot factory should be registered after Initialize");
            Assert.That(endpointSlot.IsFactoryRegistered, Is.False, "Endpoint slot factory should NOT be registered by satellite receiver");
        }

        await wrapped.StopReceive(CancellationToken.None);

        Assert.That(satelliteSlot.IsFactoryRegistered, Is.False, "Satellite slot factory should be unregistered after StopReceive");
    }

    [Test]
    public async Task Should_register_slot_factory_for_instance_receiver_on_initialize()
    {
        var endpointSlot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "green");
        var receiverSlot = new EndpointReceiverLogSlot(endpointSlot, "InstanceSpecific");
        var fakeFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new MicrosoftLoggerFactoryAdapter(fakeFactory);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, receiverSlot, slotFactory, manageSlotLifecycle: true);

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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(receiverSlot.IsFactoryRegistered, Is.True, "Receiver slot factory should be registered after Initialize");
            Assert.That(endpointSlot.IsFactoryRegistered, Is.False, "Endpoint slot factory should NOT be registered by instance receiver");
        }

        await wrapped.StopReceive(CancellationToken.None);

        Assert.That(receiverSlot.IsFactoryRegistered, Is.False, "Receiver slot factory should be unregistered after StopReceive");
    }

    [Test]
    public async Task Should_not_register_slot_factory_for_endpoint_log_slot()
    {
        var endpointSlot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var fakeFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new MicrosoftLoggerFactoryAdapter(fakeFactory);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, endpointSlot, slotFactory, manageSlotLifecycle: false);

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

        Assert.That(endpointSlot.IsFactoryRegistered, Is.False, "Endpoint slot should not self-register via LogWrappedMessageReceiver");
    }

    [Test]
    public async Task Should_route_satellite_logs_to_registered_slot_factory()
    {
        var endpointSlot = new EndpointLogSlot($"Shipping-{Guid.NewGuid():N}", "green");
        var satelliteSlot = new EndpointSatelliteLogSlot(endpointSlot, "TimeoutMigration");
        var fakeFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new MicrosoftLoggerFactoryAdapter(fakeFactory);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, satelliteSlot, slotFactory, manageSlotLifecycle: true);

        var loggerName = $"{nameof(LogWrappedMessageReceiverTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        await wrapped.Initialize(new PushRuntimeSettings(1), (messageContext, ct) =>
        {
            logger.Info("satellite-processing-log");
            return Task.CompletedTask;
        }, (errorContext, ct) =>
        {
            _ = errorContext;
            _ = ct;
            return Task.FromResult(ErrorHandleResult.Handled);
        });

        await receiver.InvokeMessage(CancellationToken.None);

        Assert.That(fakeFactory.CapturedMessages, Does.Contain("satellite-processing-log"),
            "Logs written during satellite message processing must reach the registered slot factory.");

        await wrapped.StopReceive(CancellationToken.None);
    }

    [Test]
    public async Task Should_unregister_satellite_slot_on_stop_receive()
    {
        var endpointSlot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var satelliteSlot = new EndpointSatelliteLogSlot(endpointSlot, "TimeoutMigration");
        var fakeFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new MicrosoftLoggerFactoryAdapter(fakeFactory);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, satelliteSlot, slotFactory, manageSlotLifecycle: true);

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

        Assert.That(satelliteSlot.IsFactoryRegistered, Is.True);

        await wrapped.StopReceive(CancellationToken.None);

        Assert.That(satelliteSlot.IsFactoryRegistered, Is.False, "Satellite slot factory should be unregistered after StopReceive");
    }

    [Test]
    public async Task Should_not_unregister_endpoint_slot_on_stop_receive()
    {
        var endpointSlot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var fakeFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new MicrosoftLoggerFactoryAdapter(fakeFactory);
        LogManager.RegisterSlotFactory(endpointSlot, slotFactory);
        var receiver = new TestReceiver();
        var wrapped = new LogWrappedMessageReceiver(receiver, endpointSlot, slotFactory, manageSlotLifecycle: false);

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

        await wrapped.StopReceive(CancellationToken.None);

        Assert.That(endpointSlot.IsFactoryRegistered, Is.True, "Endpoint slot should NOT be unregistered by LogWrappedMessageReceiver");

        LogManager.UnregisterSlot(endpointSlot);
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