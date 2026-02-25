#nullable enable

namespace NServiceBus.Core.Tests.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
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

        var scope = loggerFactory.Logger.CapturedScopes.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(scope.Count, Is.EqualTo(2));
            Assert.That(scope[0].Key, Is.EqualTo("Endpoint"));
            Assert.That(scope[0].Value, Is.EqualTo("Sales"));
            Assert.That(scope[1].Key, Is.EqualTo("EndpointIdentifier"));
            Assert.That(scope[1].Value, Is.EqualTo("blue"));
        }
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

        var scope = loggerFactory.Logger.CapturedScopes.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(scope.Count, Is.EqualTo(1));
            Assert.That(scope[0].Key, Is.EqualTo("Endpoint"));
            Assert.That(scope[0].Value, Is.EqualTo("Billing"));
        }
    }

    sealed class CollectingMicrosoftLoggerFactory : Microsoft.Extensions.Logging.ILoggerFactory
    {
        public CollectingMicrosoftLogger Logger { get; } = new();

        public void AddProvider(Microsoft.Extensions.Logging.ILoggerProvider provider)
        {
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => Logger;

        public void Dispose()
        {
        }
    }

    sealed class CollectingMicrosoftLogger : Microsoft.Extensions.Logging.ILogger
    {
        public List<IReadOnlyList<KeyValuePair<string, object?>>> CapturedScopes { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (state is IReadOnlyList<KeyValuePair<string, object?>> scope)
            {
                CapturedScopes.Add(scope);
            }

            return NullScope.Instance;
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
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