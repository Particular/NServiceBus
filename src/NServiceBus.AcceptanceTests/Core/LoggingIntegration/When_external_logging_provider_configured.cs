#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

public class When_external_logging_provider_configured : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_external_provider_instead_of_default()
    {
        var customProvider = new CollectingLoggerProvider();

        await Scenario.Define<Context>()
            .WithServices(services => services.AddSingleton<ILoggerProvider>(customProvider))
            .WithEndpoint<EndpointWithExternalLogging>()
            .Done(c => c.EndpointsStarted)
            .Run();

        // The custom provider should have received logs
        Assert.That(customProvider.LogEntries, Is.Not.Empty, "External provider should receive logs");
    }

    class Context : ScenarioContext;

    class EndpointWithExternalLogging : EndpointConfigurationBuilder
    {
        public EndpointWithExternalLogging() =>
            EndpointSetup<DefaultServer>();
    }

    class CollectingLoggerProvider : ILoggerProvider
    {
        public readonly List<(string Category, LogLevel Level, string Message)> LogEntries = [];

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(this, categoryName);

        public void Dispose() { }

        class CollectingLogger(CollectingLoggerProvider provider, string category) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (provider.LogEntries)
                {
                    provider.LogEntries.Add((category, logLevel, formatter(state, exception)));
                }
            }
        }
    }
}