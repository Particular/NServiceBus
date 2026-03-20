#nullable enable

namespace NServiceBus.Core.Tests.Logging;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using NServiceBus.Logging;
using NUnit.Framework;

[TestFixture]
public class EndpointLoggingScopeTests
{
    [TearDown]
    public void Cleanup()
    {
#pragma warning disable CS0618 // Test cleanup intentionally resets global LogManager default factory
        LogManager.Use<DefaultFactory>();
#pragma warning restore CS0618
    }

    [Test]
    public void Should_include_endpoint_name_and_identifier_for_multi_hosted_endpoints()
    {
        var loggerFactory = new CollectingMicrosoftLoggerFactory();
        var slot = new EndpointLogSlot("Sales", "blue");
        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(loggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("message");
        }

        AssertScopeWasUsed(loggerFactory.Logger.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Sales"),
            new KeyValuePair<string, object>("EndpointIdentifier", "blue"));
    }

    [Test]
    public void Should_include_only_endpoint_name_when_identifier_is_not_provided()
    {
        var loggerFactory = new CollectingMicrosoftLoggerFactory();
        var slot = new EndpointLogSlot("Billing", endpointIdentifier: null);
        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(loggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("message");
        }

        AssertScopeWasUsed(loggerFactory.Logger.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Billing"));
    }

    [Test]
    public void Should_include_satellite_name_for_satellite_scope()
    {
        var loggerFactory = new CollectingMicrosoftLoggerFactory();
        var endpointSlot = new EndpointLogSlot("Shipping", "green");
        var satelliteSlot = new EndpointSatelliteLogSlot(endpointSlot, "TimeoutMigration");
        LogManager.RegisterSlotFactory(satelliteSlot, new MicrosoftLoggerFactoryAdapter(loggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(satelliteSlot))
        {
            logger.Info("message");
        }

        AssertScopeWasUsed(loggerFactory.Logger.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Shipping"),
            new KeyValuePair<string, object>("EndpointIdentifier", "green"),
            new KeyValuePair<string, object>("Satellite", "TimeoutMigration"));
    }

    [Test]
    public void Should_include_receiver_name_for_instance_specific_receiver_scope()
    {
        var loggerFactory = new CollectingMicrosoftLoggerFactory();
        var endpointSlot = new EndpointLogSlot("Shipping", "green");
        var receiverSlot = new EndpointReceiverLogSlot(endpointSlot, "InstanceSpecific");
        LogManager.RegisterSlotFactory(receiverSlot, new MicrosoftLoggerFactoryAdapter(loggerFactory));

        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(receiverSlot))
        {
            logger.Info("message");
        }

        AssertScopeWasUsed(loggerFactory.Logger.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Shipping"),
            new KeyValuePair<string, object>("EndpointIdentifier", "green"),
            new KeyValuePair<string, object>("Receiver", "InstanceSpecific"));
    }

    [Test]
    public void Should_apply_scope_when_using_microsoft_logger_directly()
    {
        var loggerFactory = new CollectingMicrosoftLoggerFactory();
        var slot = new EndpointLogSlot("Sales", "blue");
        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(loggerFactory));
        var logger = loggerFactory.CreateLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(slot))
        {
            logger.LogInformation("message");
        }

        AssertScopeWasUsed(loggerFactory.Logger.CapturedLogScopes,
            new KeyValuePair<string, object>("Endpoint", "Sales"),
            new KeyValuePair<string, object>("EndpointIdentifier", "blue"));
    }

    [Test]
    public void Should_self_flush_deferred_logs_on_next_write_when_logger_was_not_present_during_explicit_flush()
    {
        var slotLoggerFactory = new CollectingMicrosoftLoggerFactory();
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

        var capturedMessages = slotLoggerFactory.Logger.CapturedMessages;
        Assert.That(capturedMessages, Does.Contain("deferred-before-registration"),
            "Deferred log must be delivered even if the explicit flush passed missed this logger.");
        Assert.That(capturedMessages, Does.Contain("written-after-registration"));
    }

    [Test]
    public void Should_flush_pending_scoped_startup_logs_to_trace_when_slot_is_unregistered_without_factory()
    {
        using var traceOutput = new StringWriter();
        using var traceListener = new TextWriterTraceListener(traceOutput);
        Trace.Listeners.Add(traceListener);

        // Slot is created but its factory is never resolved (simulates a failed endpoint startup).
        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("startup-log");
        }

        LogManager.UnregisterSlot(slot);

        traceListener.Flush();
        Trace.Listeners.Remove(traceListener);

        Assert.That(traceOutput.ToString(), Does.Contain("Info: startup-log"));
    }

    [Test]
    public void Should_not_duplicate_trace_flush_when_slot_is_unregistered_multiple_times()
    {
        using var traceOutput = new StringWriter();
        using var traceListener = new TextWriterTraceListener(traceOutput);
        Trace.Listeners.Add(traceListener);

        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var logger = LogManager.GetLogger($"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}");

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("startup-log");
        }

        LogManager.UnregisterSlot(slot);
        LogManager.UnregisterSlot(slot);

        traceListener.Flush();
        Trace.Listeners.Remove(traceListener);

        var output = traceOutput.ToString();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(output, Does.Contain("Info: startup-log"));
            Assert.That(CountOccurrences(output, "Info: startup-log"), Is.EqualTo(1));
        }
    }

    [Test]
    public void Should_route_unscoped_logs_to_default_after_slot_is_unregistered()
    {
        var slotLoggerFactory = new CollectingMicrosoftLoggerFactory();
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
            Assert.That(slotLoggerFactory.Logger.CapturedLogScopes, Is.Empty);
            Assert.That(defaultLoggerFactory.GetMessages(loggerName), Is.EqualTo(["after-unregister"]));
        }
    }

    [Test]
    public void Should_keep_deferred_startup_logs_isolated_per_slot_when_multiple_endpoints_start_concurrently()
    {
        var salesLoggerFactory = new CollectingMicrosoftLoggerFactory();
        var billingLoggerFactory = new CollectingMicrosoftLoggerFactory();
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
            Assert.That(salesLoggerFactory.Logger.CapturedMessages, Does.Contain("sales-startup-log"));
            Assert.That(salesLoggerFactory.Logger.CapturedMessages, Does.Not.Contain("billing-startup-log"));
            Assert.That(billingLoggerFactory.Logger.CapturedMessages, Does.Contain("billing-startup-log"));
            Assert.That(billingLoggerFactory.Logger.CapturedMessages, Does.Not.Contain("sales-startup-log"));
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

    sealed class CollectingMicrosoftLoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory
    {
        public CollectingMicrosoftLogger Logger { get; } = new();

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => Logger;

        public void Dispose()
        {
        }
    }

    sealed class CollectingMicrosoftLogger : ILogger
    {
        public List<IReadOnlyList<KeyValuePair<string, object>>> CapturedLogScopes { get; } = [];
        public List<string> CapturedMessages { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (state is not IReadOnlyList<KeyValuePair<string, object>> scope)
            {
                return NullScope.Instance;
            }

            var currentScopes = activeScopes.Value ??= new Stack<IReadOnlyList<KeyValuePair<string, object>>>();
            currentScopes.Push(scope);
            return new Scope(currentScopes);

        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            CapturedMessages.Add(formatter(state, exception));
            if (activeScopes.Value is { Count: > 0 } currentScopes)
            {
                CapturedLogScopes.Add(currentScopes.Peek());
            }
        }

        readonly AsyncLocal<Stack<IReadOnlyList<KeyValuePair<string, object>>>> activeScopes = new();

        sealed class Scope(Stack<IReadOnlyList<KeyValuePair<string, object>>> currentScopes) : IDisposable
        {
            public void Dispose() => currentScopes.Pop();
        }

        sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
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

    static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}