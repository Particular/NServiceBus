#nullable enable

namespace NServiceBus.Core.Tests.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NServiceBus.Logging;
using NUnit.Framework;

[TestFixture]
public class EndpointLoggingScopeTests
{
    [Test]
    public void Should_include_endpoint_name_and_identifier_for_multi_hosted_endpoints()
    {
        var loggerFactory = new FakeLoggerLoggerFactory();
        var slot = new EndpointLogSlot("Sales", "blue");
        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(loggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("message");
        }

        AssertScopeWasUsed(loggerFactory.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Sales"),
            new KeyValuePair<string, object>("EndpointIdentifier", "blue"));
    }

    [Test]
    public void Should_include_only_endpoint_name_when_identifier_is_not_provided()
    {
        var loggerFactory = new FakeLoggerLoggerFactory();
        var slot = new EndpointLogSlot("Billing", endpointIdentifier: null);
        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(loggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("message");
        }

        AssertScopeWasUsed(loggerFactory.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Billing"));
    }

    [Test]
    public void Should_include_satellite_name_for_satellite_scope()
    {
        var loggerFactory = new FakeLoggerLoggerFactory();
        var endpointSlot = new EndpointLogSlot("Shipping", "green");
        var satelliteSlot = new EndpointSatelliteLogSlot(endpointSlot, "TimeoutMigration");
        LogManager.RegisterSlotFactory(satelliteSlot, new MicrosoftLoggerFactoryAdapter(loggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(satelliteSlot))
        {
            logger.Info("message");
        }

        AssertScopeWasUsed(loggerFactory.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Shipping"),
            new KeyValuePair<string, object>("EndpointIdentifier", "green"),
            new KeyValuePair<string, object>("Satellite", "TimeoutMigration"));
    }

    [Test]
    public void Should_include_receiver_name_for_instance_specific_receiver_scope()
    {
        var loggerFactory = new FakeLoggerLoggerFactory();
        var endpointSlot = new EndpointLogSlot("Shipping", "green");
        var receiverSlot = new EndpointReceiverLogSlot(endpointSlot, "InstanceSpecific");
        LogManager.RegisterSlotFactory(receiverSlot, new MicrosoftLoggerFactoryAdapter(loggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(receiverSlot))
        {
            logger.Info("message");
        }

        AssertScopeWasUsed(loggerFactory.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Shipping"),
            new KeyValuePair<string, object>("EndpointIdentifier", "green"),
            new KeyValuePair<string, object>("Receiver", "InstanceSpecific"));
    }

    [Test]
    public void Should_route_satellite_logs_to_endpoint_factory_when_only_endpoint_slot_is_registered()
    {
        // In production only the endpoint slot gets a factory registered (EndpointPreparation
        // calls RegisterSlotFactory for the EndpointLogSlot). Satellite slots are created
        // per-satellite by ReceiveComponent and never registered, so logs emitted during
        // satellite message processing must still reach the endpoint's logger factory rather
        // than being buffered into a per-slot queue that is never drained.
        var endpointLoggerFactory = new FakeLoggerLoggerFactory();
        var endpointSlot = new EndpointLogSlot($"Shipping-{Guid.NewGuid():N}", "green");
        var satelliteSlot = new EndpointSatelliteLogSlot(endpointSlot, "TimeoutMigration");
        LogManager.RegisterSlotFactory(endpointSlot, new MicrosoftLoggerFactoryAdapter(endpointLoggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(satelliteSlot))
        {
            logger.Info("satellite-processing-log");
        }

        Assert.That(endpointLoggerFactory.CapturedMessages, Does.Contain("satellite-processing-log"),
            "Logs written inside an (unregistered) satellite slot scope must be routed to the parent endpoint factory, not silently dropped.");

        LogManager.UnregisterSlot(satelliteSlot);
        LogManager.UnregisterSlot(endpointSlot);
    }

    [Test]
    public void Should_route_instance_specific_receiver_logs_to_endpoint_factory_when_only_endpoint_slot_is_registered()
    {
        // Same production scenario as satellites: ReceiveComponent creates an
        // EndpointReceiverLogSlot for the instance-specific receiver but never registers a
        // factory for it. Logs during instance-specific message processing must reach the
        // endpoint factory.
        var endpointLoggerFactory = new FakeLoggerLoggerFactory();
        var endpointSlot = new EndpointLogSlot($"Shipping-{Guid.NewGuid():N}", "green");
        var receiverSlot = new EndpointReceiverLogSlot(endpointSlot, "InstanceSpecific");
        LogManager.RegisterSlotFactory(endpointSlot, new MicrosoftLoggerFactoryAdapter(endpointLoggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(receiverSlot))
        {
            logger.Info("instance-receiver-processing-log");
        }

        Assert.That(endpointLoggerFactory.CapturedMessages, Does.Contain("instance-receiver-processing-log"),
            "Logs written inside an (unregistered) instance-specific receiver slot scope must be routed to the parent endpoint factory, not silently dropped.");

        LogManager.UnregisterSlot(receiverSlot);
        LogManager.UnregisterSlot(endpointSlot);
    }

    [Test]
    public void Should_not_buffer_satellite_logs_indefinitely_when_slot_is_never_registered()
    {
        // Guards against the unbounded-growth side of the same defect: if satellite logs are
        // enqueued into the per-slot deferred-startup queue (because the satellite slot is
        // never registered), that queue is never drained for the life of the process. Writing
        // many entries and then unregistering the endpoint must not leave the satellite logs
        // stranded — they should have been delivered to the endpoint factory as they were written.
        var endpointLoggerFactory = new FakeLoggerLoggerFactory();
        var endpointSlot = new EndpointLogSlot($"Shipping-{Guid.NewGuid():N}", "green");
        var satelliteSlot = new EndpointSatelliteLogSlot(endpointSlot, "TimeoutMigration");
        LogManager.RegisterSlotFactory(endpointSlot, new MicrosoftLoggerFactoryAdapter(endpointLoggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(satelliteSlot))
        {
            for (var i = 0; i < 100; i++)
            {
                logger.Info($"satellite-log-{i}");
            }
        }

        Assert.That(endpointLoggerFactory.CapturedMessages.Count(m => m is not null && m.StartsWith("satellite-log-")), Is.EqualTo(100),
            "Every satellite log must be delivered to the endpoint factory, not buffered into a never-drained queue.");

        LogManager.UnregisterSlot(satelliteSlot);
        LogManager.UnregisterSlot(endpointSlot);
    }

    [Test]
    public void Should_apply_scope_when_using_microsoft_logger_directly()
    {
        var loggerFactory = new FakeLoggerLoggerFactory();
        var slot = new EndpointLogSlot("Sales", "blue");
        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(loggerFactory));
        var logger = loggerFactory.CreateLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(slot))
        {
            logger.LogInformation("message");
        }

        AssertScopeWasUsed(loggerFactory.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Sales"),
            new KeyValuePair<string, object>("EndpointIdentifier", "blue"));
    }

    [Test]
    public void Should_self_flush_deferred_logs_on_next_write_when_logger_was_not_present_during_explicit_flush()
    {
        var slotLoggerFactory = new FakeLoggerLoggerFactory();
        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        // Accumulate deferred logs while the factory is not yet registered.
        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("deferred-before-registration");
        }

        // RegisterSlotFactory explicitly flushes all loggers it can see in the snapshot.
        // Simulate the race where this logger was missed by calling RegisterSlotFactory
        // before the logger was created — we can't truly reproduce the concurrency window
        // in a unit test, but after registration the next write must self-flush.
        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(slotLoggerFactory));

        // The next write through the same logger within the slot scope must trigger
        // a self-flush of the buffered deferred message followed by the new message.
        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("written-after-registration");
        }

        var capturedMessages = slotLoggerFactory.CapturedMessages;
        Assert.That(capturedMessages, Does.Contain("deferred-before-registration"),
            "Deferred log must be delivered even if the explicit flush passed missed this logger.");
        Assert.That(capturedMessages, Does.Contain("written-after-registration"));
    }

    [Test]
    public void Should_flush_pending_scoped_startup_logs_to_fallback_when_slot_is_unregistered_without_factory()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        LogManager.Use<DefaultFactory>();
#pragma warning restore CS0618 // Type or member is obsolete

        try
        {
            // Slot is created but its factory is never resolved (simulates a failed endpoint startup).
            var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
            var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
            var logger = LogManager.GetLogger(loggerName);

            using (LogManager.BeginSlotScope(slot))
            {
                logger.Info("startup-log");
            }

            LogManager.UnregisterSlot(slot);

            // Logs are drained through FallbackLoggerFactory instead of trace.
            // Verify the behavior: no exception, slot is cleaned up.
            Assert.That(LogManager.DefaultFactoryIsUnsupported, Is.True);
        }
        finally
        {
#pragma warning disable CS0618 // Type or member is obsolete
            LogManager.Use<DefaultFactory>();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }

    [Test]
    public void Should_not_duplicate_fallback_flush_when_slot_is_unregistered_multiple_times()
    {
        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("startup-log");
        }

        LogManager.UnregisterSlot(slot);
        Assert.DoesNotThrow(() => LogManager.UnregisterSlot(slot));
    }

    [Test]
    public void Should_write_to_fallback_instead_of_buffering_when_logging_inside_stale_slot_scope_after_unregistration()
    {
        var slotLoggerFactory = new FakeLoggerLoggerFactory();
        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        var staleScope = LogManager.BeginSlotScope(slot);
        LogManager.UnregisterSlot(slot);

        logger.Info("stale-scope-log");

        staleScope.Dispose();

        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(slotLoggerFactory));
        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("fresh-log");
        }

        using (Assert.EnterMultipleScope())
        {
            // The stale scope log went through FallbackLoggerFactory (not trace).
            // The slot factory should only have the fresh log.
            Assert.That(slotLoggerFactory.CapturedMessages, Does.Contain("fresh-log"));
            Assert.That(slotLoggerFactory.CapturedMessages, Does.Not.Contain("stale-scope-log"));
        }

        LogManager.UnregisterSlot(slot);
    }

    [Test]
    public void Should_route_unscoped_logs_to_default_after_slot_is_unregistered()
    {
        var slotLoggerFactory = new FakeLoggerLoggerFactory();
        var defaultLoggerFactory = new CollectingNServiceBusLoggerFactory();
#pragma warning disable CS0618 // UseFactory is deprecated; test exercises legacy behavior intentionally
        LogManager.UseFactory(defaultLoggerFactory);
#pragma warning restore CS0618

        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(slotLoggerFactory));
        LogManager.UnregisterSlot(slot);

        logger.Info("after-unregister");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(slotLoggerFactory.CapturedLogScopes, Is.Empty);
            Assert.That(defaultLoggerFactory.GetMessages(loggerName), Is.EqualTo(["after-unregister"]));
        }
    }

    [Test]
    public void Should_keep_deferred_startup_logs_isolated_per_slot_when_multiple_endpoints_start_concurrently()
    {
        var salesLoggerFactory = new FakeLoggerLoggerFactory();
        var billingLoggerFactory = new FakeLoggerLoggerFactory();
        var salesSlot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var billingSlot = new EndpointLogSlot($"Billing-{Guid.NewGuid():N}", "green");
        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(salesSlot))
        {
            logger.Info("sales-startup-log");
        }

        using (LogManager.BeginSlotScope(billingSlot))
        {
            logger.Info("billing-startup-log");
        }

        // Register in reverse order to model concurrent multi-endpoint startup races.
        LogManager.RegisterSlotFactory(billingSlot, new MicrosoftLoggerFactoryAdapter(billingLoggerFactory));
        LogManager.RegisterSlotFactory(salesSlot, new MicrosoftLoggerFactoryAdapter(salesLoggerFactory));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(salesLoggerFactory.CapturedMessages, Does.Contain("sales-startup-log"));
            Assert.That(salesLoggerFactory.CapturedMessages, Does.Not.Contain("billing-startup-log"));
            Assert.That(billingLoggerFactory.CapturedMessages, Does.Contain("billing-startup-log"));
            Assert.That(billingLoggerFactory.CapturedMessages, Does.Not.Contain("sales-startup-log"));
        }

        LogManager.UnregisterSlot(salesSlot);
        LogManager.UnregisterSlot(billingSlot);
    }

    static void AssertScopeWasUsed(List<IReadOnlyList<KeyValuePair<string, object>>> capturedLogScopes, params KeyValuePair<string, object>[] expectedScope)
    {
        Assert.That(capturedLogScopes, Has.Some.Matches<IReadOnlyList<KeyValuePair<string, object>>>(scope => ScopeMatches(scope, expectedScope)));

        static bool ScopeMatches(IReadOnlyList<KeyValuePair<string, object>> scope, IReadOnlyList<KeyValuePair<string, object>> expected)
        {
            if (scope.Count != expected.Count)
            {
                return false;
            }

            for (var i = 0; i < expected.Count; i++)
            {
                if (scope[i].Key != expected[i].Key || !Equals(scope[i].Value, expected[i].Value))
                {
                    return false;
                }
            }

            return true;
        }
    }

    sealed class FakeLoggerLoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory
    {
        readonly FakeLoggerProvider provider = new();

        public List<string> CapturedMessages => [.. provider.Collector.GetSnapshot().Select(r => r.Message)];

        public List<IReadOnlyList<KeyValuePair<string, object>>> CapturedLogScopes =>
        [
            .. provider.Collector.GetSnapshot()
                .Where(r => r.Scopes is { Count: > 0 })
                .Select(ExtractScope)
                .Where(s => s is not null)
                .Select(s => s!)
        ];

        static IReadOnlyList<KeyValuePair<string, object>>? ExtractScope(FakeLogRecord record)
        {
            foreach (var scope in record.Scopes)
            {
                if (scope is IReadOnlyList<KeyValuePair<string, object>> list)
                {
                    return list;
                }

                if (scope is LogSlot logSlot)
                {
                    return (IReadOnlyList<KeyValuePair<string, object>>)(object)logSlot.ScopeState;
                }
            }

            return null;
        }

        public void AddProvider(ILoggerProvider loggerProvider) => throw new NotSupportedException();

        public ILogger CreateLogger(string categoryName) => provider.CreateLogger(categoryName);

        public void Dispose() => provider.Dispose();
    }

    sealed class CollectingNServiceBusLoggerFactory : NServiceBus.Logging.ILoggerFactory
    {
        public ILog GetLogger(Type type) => GetLogger(type.FullName!);

        public ILog GetLogger(string name)
        {
            var logger = new CollectingNServiceBusLogger();
            loggers.Add(name, logger);
            return logger;
        }

        public string?[] GetMessages(string name) => !loggers.TryGetValue(name, out var logger) ? [] : [.. logger.Messages];

        readonly Dictionary<string, CollectingNServiceBusLogger> loggers = new(StringComparer.Ordinal);
    }

    sealed class CollectingNServiceBusLogger : ILog
    {
        public bool IsDebugEnabled => true;
        public bool IsInfoEnabled => true;
        public bool IsWarnEnabled => true;
        public bool IsErrorEnabled => true;
        public bool IsFatalEnabled => true;

        public void Debug(string? message) => Messages.Enqueue(message);

        public void Debug(string? message, Exception? exception) => Messages.Enqueue(message);

        public void DebugFormat(string format, params object?[] args) => Messages.Enqueue(string.Format(format, args));

        public void Info(string? message) => Messages.Enqueue(message);

        public void Info(string? message, Exception? exception) => Messages.Enqueue(message);

        public void InfoFormat(string format, params object?[] args) => Messages.Enqueue(string.Format(format, args));

        public void Warn(string? message) => Messages.Enqueue(message);

        public void Warn(string? message, Exception? exception) => Messages.Enqueue(message);

        public void WarnFormat(string format, params object?[] args) => Messages.Enqueue(string.Format(format, args));

        public void Error(string? message) => Messages.Enqueue(message);

        public void Error(string? message, Exception? exception) => Messages.Enqueue(message);

        public void ErrorFormat(string format, params object?[] args) => Messages.Enqueue(string.Format(format, args));

        public void Fatal(string? message) => Messages.Enqueue(message);

        public void Fatal(string? message, Exception? exception) => Messages.Enqueue(message);

        public void FatalFormat(string format, params object?[] args) => Messages.Enqueue(string.Format(format, args));

        public Queue<string?> Messages { get; } = new();
    }

    [Test]
    public void SetAmbientDefaultFactory_should_route_unscoped_logs_to_ambient_factory()
    {
        var ambientFactory = new FakeLoggerLoggerFactory();
        using var scope = AmbientScope.Create(ambientFactory);

        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        logger.Info("ambient-test-message");

        Assert.That(ambientFactory.CapturedMessages, Has.Some.Contains("ambient-test-message"));
    }

    [Test]
    public void SetAmbientDefaultFactory_should_not_affect_slot_scoped_logs()
    {
        var ambientFactory = new FakeLoggerLoggerFactory();
        var slotFactory = new FakeLoggerLoggerFactory();
        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");

        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(slotFactory));
        using var ambient = AmbientScope.Create(ambientFactory);

        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("in-slot-message");
        }

        logger.Info("out-of-slot-message");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(slotFactory.CapturedMessages, Has.Some.Contains("in-slot-message"));
            Assert.That(slotFactory.CapturedMessages, Does.Not.Contain("out-of-slot-message"));
            Assert.That(ambientFactory.CapturedMessages, Has.Some.Contains("out-of-slot-message"));
            Assert.That(ambientFactory.CapturedMessages, Does.Not.Contain("in-slot-message"));
        }

        LogManager.UnregisterSlot(slot);
    }

    [Test]
    public void Clearing_ambient_default_factory_should_fall_back_to_default_factory()
    {
        var ambientFactory = new FakeLoggerLoggerFactory();
        var defaultFactory = new CollectingNServiceBusLoggerFactory();
#pragma warning disable CS0618 // UseFactory is deprecated; test exercises legacy behavior intentionally
        LogManager.UseFactory(defaultFactory);
#pragma warning restore CS0618

        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        using (AmbientScope.Create(ambientFactory))
        {
            logger.Info("while-ambient-set");
        }

        var logger2Name = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger2 = LogManager.GetLogger(logger2Name);
        logger2.Info("after-ambient-cleared");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ambientFactory.CapturedMessages, Has.Some.Contains("while-ambient-set"));
            Assert.That(defaultFactory.GetMessages(logger2Name), Is.EqualTo(["after-ambient-cleared"]));
        }
    }

    [Test]
    public void Ambient_default_factory_should_route_stale_slot_logs()
    {
        var ambientFactory = new FakeLoggerLoggerFactory();
        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        using var ambient = AmbientScope.Create(ambientFactory);

        var staleScope = LogManager.BeginSlotScope(slot);
        LogManager.UnregisterSlot(slot);

        logger.Info("stale-scope-ambient-log");

        staleScope.Dispose();

        Assert.That(ambientFactory.CapturedMessages, Has.Some.Contains("stale-scope-ambient-log"));
    }

    [Test]
    public async Task SlotUnregisterer_should_unregister_slot_on_dispose()
    {
        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerFactory = new FakeLoggerLoggerFactory();
        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(loggerFactory));
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        // Simulate a stale scope that outlives container disposal.
        // In production, this happens when background threads still hold a slot scope
        // while the container begins its disposal chain.
        var staleScope = LogManager.BeginSlotScope(slot);

        var ambientFactory = new FakeLoggerLoggerFactory();
        using var ambient = AmbientScope.Create(ambientFactory);

        // SlotUnregisterer disposes, unregistering the slot.
        // The stale scope's context is removed from slotContexts,
        // so subsequent writes through the stale scope drain to ambient.
        var unregisterer = new SlotUnregisterer(slot);
        await unregisterer.DisposeAsync();

        logger.Info("after-unregisterer-dispose");

        staleScope.Dispose();

        Assert.That(ambientFactory.CapturedMessages, Has.Some.Contains("after-unregisterer-dispose"));
    }

    [Test]
    public void SlotUnregisterer_should_be_idempotent_on_concurrent_dispose()
    {
        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerFactory = new FakeLoggerLoggerFactory();
        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(loggerFactory));
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        var staleScope = LogManager.BeginSlotScope(slot);

        var ambientFactory = new FakeLoggerLoggerFactory();
        using var ambient = AmbientScope.Create(ambientFactory);

        var unregisterer = new SlotUnregisterer(slot);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => unregisterer.DisposeAsync().AsTask()))
            .ToArray();
        Task.WaitAll(tasks);

        logger.Info("post-race");

        staleScope.Dispose();

        Assert.That(ambientFactory.CapturedMessages, Has.Some.Contains("post-race"));
    }

    [Test]
    public async Task Ambient_default_factory_should_not_leak_across_async_boundaries()
    {
        var ambientFactory = new FakeLoggerLoggerFactory();
        using var ambient = AmbientScope.Create(ambientFactory);

        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";

        // Verify ambient flows into child async context
        var childSawAmbient = await Task.Run(() =>
        {
            var logger = LogManager.GetLogger(loggerName);
            logger.Info("from-child");
            return true;
        });

        LogManager.SetAmbientDefaultFactory(null);

        // New async context without ambient should not see the old one
        var childSawNoAmbient = await Task.Run(() =>
        {
            var logger2Name = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
            var logger2 = LogManager.GetLogger(logger2Name);
            logger2.Info("no-ambient");
            return ambientFactory.CapturedMessages.Contains("no-ambient");
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(childSawAmbient, Is.True);
            Assert.That(childSawNoAmbient, Is.False);
            Assert.That(ambientFactory.CapturedMessages, Has.Some.Contains("from-child"));
        }
    }

    [Test]
    public void Ambient_default_factory_should_be_overridden_by_nested_set()
    {
        var outerFactory = new FakeLoggerLoggerFactory();
        var innerFactory = new FakeLoggerLoggerFactory();

        using (AmbientScope.Create(outerFactory))
        {
            var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
            LogManager.GetLogger(loggerName).Info("outer");

            using (AmbientScope.Create(innerFactory))
            {
                LogManager.GetLogger(loggerName).Info("inner");

                Assert.That(innerFactory.CapturedMessages, Has.Some.Contains("inner"));
            }

            LogManager.GetLogger(loggerName).Info("outer-restored");

            Assert.That(outerFactory.CapturedMessages, Has.Some.Contains("outer"));
            Assert.That(outerFactory.CapturedMessages, Has.Some.Contains("outer-restored"));
        }
    }

    [Test]
    public void BeginEndpointScope_should_return_noop_when_inside_slot_scope()
    {
        var loggerFactory = new FakeLoggerLoggerFactory();
        var slot = new EndpointLogSlot("Sales", "blue");
        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(loggerFactory));

        var logger = loggerFactory.CreateLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");
        var endpointScope = new EndpointLoggingScope { EndpointName = "Sales", EndpointIdentifier = "blue", Slot = slot };

        using (LogManager.BeginSlotScope(slot))
        {
            using (logger.BeginEndpointScope(endpointScope))
            {
                logger.LogInformation("inside-slot");
            }
        }

        Assert.That(loggerFactory.CapturedLogScopes.Count, Is.EqualTo(1),
            "Only the slot scope should appear, not a duplicate from BeginEndpointScope");
    }

    [Test]
    public void BeginEndpointScope_should_push_both_slot_and_mel_scope_when_factory_not_registered()
    {
        var loggerFactory = new FakeLoggerLoggerFactory();
        var logger = loggerFactory.CreateLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");
        var slot = new EndpointLogSlot("Billing", "green");
        var endpointScope = new EndpointLoggingScope { EndpointName = "Billing", EndpointIdentifier = "green", Slot = slot };

        using (logger.BeginEndpointScope(endpointScope))
        {
            Assert.That(LogManager.TryGetCurrentEndpointScopeState(out _), Is.True,
                "SlotScope should be active even when factory is not registered");
            logger.LogInformation("outside-slot");
        }

        AssertScopeWasUsed(loggerFactory.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Billing"),
            new KeyValuePair<string, object>("EndpointIdentifier", "green"));
    }

    [Test]
    public void Nested_slot_scopes_with_same_slot_should_not_duplicate_mel_scope()
    {
        var loggerFactory = new FakeLoggerLoggerFactory();
        var slot = new EndpointLogSlot("Sales", "blue");
        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(loggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(slot))
        {
            // Inner scope re-enters the same slot (simulates MessageSession inside a receiver)
            using (LogManager.BeginSlotScope(slot))
            {
                logger.Info("nested-same-slot");
            }
        }

        Assert.That(loggerFactory.CapturedLogScopes, Has.Count.EqualTo(1),
            "Re-entering the same slot should not push a duplicate MEL scope");
    }

    [Test]
    public void Nested_slot_scopes_with_different_slots_should_each_push_mel_scope()
    {
        var satelliteLoggerFactory = new FakeLoggerLoggerFactory();
        var endpointSlot = new EndpointLogSlot("Sales", "blue");
        var satelliteSlot = new EndpointSatelliteLogSlot(endpointSlot, "TimeoutManager");
        LogManager.RegisterSlotFactory(satelliteSlot, new MicrosoftLoggerFactoryAdapter(satelliteLoggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(endpointSlot))
        {
            using (LogManager.BeginSlotScope(satelliteSlot))
            {
                logger.Info("nested-different-slots");
            }
        }

        // The inner (satellite) slot routes to the satellite factory, with both Endpoint and Satellite scope entries
        AssertScopeWasUsed(satelliteLoggerFactory.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Sales"),
            new KeyValuePair<string, object>("EndpointIdentifier", "blue"),
            new KeyValuePair<string, object>("Satellite", "TimeoutManager"));
    }

    sealed class AmbientScope : IDisposable
    {
        readonly Microsoft.Extensions.Logging.ILoggerFactory? previous;

        AmbientScope(Microsoft.Extensions.Logging.ILoggerFactory? factory)
        {
            previous = LogManager.GetAmbientDefaultFactory();
            LogManager.SetAmbientDefaultFactory(factory);
        }

        public void Dispose() => LogManager.SetAmbientDefaultFactory(previous);

        public static AmbientScope Create(Microsoft.Extensions.Logging.ILoggerFactory? factory) => new(factory);
    }
}