#nullable enable

namespace NServiceBus.Core.Tests.Logging;

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using NServiceBus.Logging;
using NUnit.Framework;

[TestFixture]
public class EndpointLoggingScopeTests
{
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
    public void Should_flush_deferred_logs_and_fall_back_to_default_logger_when_slot_factory_is_unavailable()
    {
        var defaultLoggerFactory = new CollectingNServiceBusLoggerFactory();
        LogManager.UseFactory(defaultLoggerFactory);

        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("before-fallback");
        }

        LogManager.MarkSlotFactoryAsUnavailable(slot);

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("after-fallback");
        }

        var expectedMessages = new[] { "before-fallback", "after-fallback" };
        Assert.That(defaultLoggerFactory.GetMessages(loggerName), Is.EqualTo(expectedMessages));
    }

    [Test]
    public void Should_not_duplicate_deferred_logs_when_slot_factory_is_marked_unavailable_multiple_times()
    {
        var defaultLoggerFactory = new CollectingNServiceBusLoggerFactory();
        LogManager.UseFactory(defaultLoggerFactory);

        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("before-fallback");
        }

        LogManager.MarkSlotFactoryAsUnavailable(slot);
        LogManager.MarkSlotFactoryAsUnavailable(slot);

        var expectedMessages = new[] { "before-fallback" };
        Assert.That(defaultLoggerFactory.GetMessages(loggerName), Is.EqualTo(expectedMessages));
    }

    [Test]
    public void Should_flush_pending_deferred_logs_to_default_logger_when_slot_is_unregistered()
    {
        var defaultLoggerFactory = new CollectingNServiceBusLoggerFactory();
        LogManager.UseFactory(defaultLoggerFactory);

        // Slot is created but its factory is never resolved (simulates a failed endpoint startup).
        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("startup-log");
        }

        LogManager.UnregisterSlot(slot);

        Assert.That(defaultLoggerFactory.GetMessages(loggerName), Is.EqualTo(["startup-log"]));
    }

    [Test]
    public void Should_not_duplicate_pending_deferred_logs_when_slot_is_unregistered_multiple_times()
    {
        var defaultLoggerFactory = new CollectingNServiceBusLoggerFactory();
        LogManager.UseFactory(defaultLoggerFactory);

        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        using (LogManager.BeginSlotScope(slot))
        {
            logger.Info("startup-log");
        }

        LogManager.UnregisterSlot(slot);
        LogManager.UnregisterSlot(slot);

        Assert.That(defaultLoggerFactory.GetMessages(loggerName), Is.EqualTo(["startup-log"]));
    }

    [Test]
    public void Should_route_logs_to_default_after_slot_is_unregistered()
    {
        var slotLoggerFactory = new CollectingMicrosoftLoggerFactory();
        var defaultLoggerFactory = new CollectingNServiceBusLoggerFactory();
        LogManager.UseFactory(defaultLoggerFactory);

        var slot = new EndpointLogSlot($"Sales-{Guid.NewGuid():N}", "blue");
        var loggerName = $"{nameof(EndpointLoggingScopeTests)}-{Guid.NewGuid():N}";
        var logger = LogManager.GetLogger(loggerName);

        LogManager.RegisterSlotFactory(slot, new MicrosoftLoggerFactoryAdapter(slotLoggerFactory));
        LogManager.UnregisterSlot(slot);

        // Log outside any slot scope — BeginSlotScope must not be used here because it
        // calls GetOrAddSlotContext which re-inserts the slot as Pending, which would
        // cause ShouldDeferSlotLogs to return true and defer the message instead of
        // writing it to the default logger.
        logger.Info("after-unregister");

        // The slot's factory is gone — the message must have gone to the default logger.
        Assert.That(slotLoggerFactory.Logger.CapturedLogScopes, Is.Empty);
        Assert.That(defaultLoggerFactory.GetMessages(loggerName), Is.EqualTo(["after-unregister"]));
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
}