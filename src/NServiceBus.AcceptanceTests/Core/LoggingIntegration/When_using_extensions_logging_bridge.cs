#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using Configuration.AdvancedExtensibility;
using EndpointTemplates;
using Logging;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

[NonParallelizable]
public class When_using_extensions_logging_bridge : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_use_external_factory_and_not_register_default_providers()
    {
        var customProvider = new CollectingLoggerProvider();

        using var externalLoggerFactory = LoggerFactory.Create(builder => builder.AddProvider(customProvider));

        // User bridges it to NServiceBus via LogManager.UseFactory (simulating ExtensionsLoggerFactory usage)
        LogManager.UseFactory(new BridgeLoggerFactory(externalLoggerFactory));

        await Scenario.Define<Context>()
            .WithEndpoint<EndpointWithBridge>(b => b.CustomConfig(c => c.GetSettings().Set(c.GetSettings().Get<Context>())))
            .Done(c => c.EndpointsStarted)
            .Run();

        Assert.That(customProvider.LogEntries, Is.Not.Empty, "External provider should receive logs via the bridge");

    }

    public class Context : ScenarioContext;

    public class EndpointWithBridge : EndpointConfigurationBuilder
    {
        public EndpointWithBridge() => EndpointSetup<DefaultServer>();
    }

    /// <summary>
    /// Simulates the ExtensionsLoggerFactory from NServiceBus.Extensions.Logging package
    /// </summary>
    class BridgeLoggerFactory(ILoggerFactory msFactory) : global::NServiceBus.Logging.ILoggerFactory
    {
        public ILog GetLogger(Type type) => new BridgeLogger(msFactory.CreateLogger(type.FullName ?? type.Name));

        public ILog GetLogger(string name) => new BridgeLogger(msFactory.CreateLogger(name));

#pragma warning disable CA2254
        class BridgeLogger(ILogger logger) : ILog
        {
            public bool IsDebugEnabled => logger.IsEnabled(LogLevel.Debug);
            public bool IsInfoEnabled => logger.IsEnabled(LogLevel.Information);
            public bool IsWarnEnabled => logger.IsEnabled(LogLevel.Warning);
            public bool IsErrorEnabled => logger.IsEnabled(LogLevel.Error);
            public bool IsFatalEnabled => logger.IsEnabled(LogLevel.Critical);

            public void Debug(string? message) => logger.LogDebug(message);
            public void Debug(string? message, Exception? exception) => logger.LogDebug(exception, message);
            public void DebugFormat(string format, params object?[] args) => logger.LogDebug(format, args);

            public void Info(string? message) => logger.LogInformation(message);
            public void Info(string? message, Exception? exception) => logger.LogInformation(exception, message);
            public void InfoFormat(string format, params object?[] args) => logger.LogInformation(format, args);

            public void Warn(string? message) => logger.LogWarning(message);
            public void Warn(string? message, Exception? exception) => logger.LogWarning(exception, message);
            public void WarnFormat(string format, params object?[] args) => logger.LogWarning(format, args);

            public void Error(string? message) => logger.LogError(message);
            public void Error(string? message, Exception? exception) => logger.LogError(exception, message);
            public void ErrorFormat(string format, params object?[] args) => logger.LogError(format, args);
            public void Fatal(string? message) => logger.LogCritical(message);
            public void Fatal(string? message, Exception? exception) => logger.LogCritical(exception, message);
            public void FatalFormat(string format, params object?[] args) => logger.LogCritical(format, args);
#pragma warning restore CA2254
        }
    }
}