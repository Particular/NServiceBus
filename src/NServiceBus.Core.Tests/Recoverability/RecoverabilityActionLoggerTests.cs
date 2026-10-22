namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Collections.Concurrent;
using System.Linq;
using Extensibility;
using Logging;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Transport;

[TestFixture]
public class RecoverabilityActionLoggerTests
{
    // The exception details are always logged with the recoverability decision; ExceptionRecordingMode only controls
    // whether the exception is also recorded as an event on the activity. The executor test covers both modes.
    [Test]
    public void Should_log_the_exception_with_the_decision()
    {
        var (logger, entries) = CreateLogger();
        var errorContext = CreateErrorContext();

        logger.LogRecoverabilityAction(RecoverabilityAction.MoveToError("error"), errorContext);

        var entry = entries.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(entry.Exception, Is.SameAs(errorContext.Exception));
            Assert.That(entry.Message, Does.StartWith($"Moving message '{errorContext.MessageId}' to the error queue 'error' because processing failed due to an exception:"));
        }
    }

    [Test]
    public void Should_log_at_the_level_of_the_decision()
    {
        var (logger, entries) = CreateLogger();
        var errorContext = CreateErrorContext();

        logger.LogRecoverabilityAction(RecoverabilityAction.ImmediateRetry(), errorContext);
        logger.LogRecoverabilityAction(RecoverabilityAction.DelayedRetry(TimeSpan.FromSeconds(1)), errorContext);
        logger.LogRecoverabilityAction(RecoverabilityAction.MoveToError("error"), errorContext);
        logger.LogRecoverabilityAction(RecoverabilityAction.Discard("reason"), errorContext);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entries.Select(e => e.Level), Is.EqualTo([LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Information]));
            Assert.That(entries.Select(e => e.Exception), Is.All.SameAs(errorContext.Exception));
        }
    }

    static (RecoverabilityActionLogger logger, ConcurrentQueue<CapturedLogEntry> entries) CreateLogger()
    {
        var (serviceProvider, entries) = CapturingLoggerProvider.CreateServiceProvider();
        return (new RecoverabilityActionLogger(serviceProvider), entries);
    }

    static ErrorContext CreateErrorContext() => new(new InvalidOperationException("boom"), [], Guid.NewGuid().ToString(), Array.Empty<byte>(), new TransportTransaction(), 1, "receive address", new ContextBag());
}
