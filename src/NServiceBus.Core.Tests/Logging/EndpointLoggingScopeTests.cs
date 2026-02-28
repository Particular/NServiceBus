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
        public List<IReadOnlyList<KeyValuePair<string, object>>> CapturedScopes { get; } = [];
        public List<IReadOnlyList<KeyValuePair<string, object>>> CapturedLogScopes { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (state is not IReadOnlyList<KeyValuePair<string, object>> scope)
            {
                return NullScope.Instance;
            }

            CapturedScopes.Add(scope);
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
}