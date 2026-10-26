namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Linq;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Testing;

[TestFixture]
public class RecoverabilityActionLoggingTests
{
    [Test]
    public void Should_log_the_exception_with_the_decision()
    {
        var (context, provider) = CreateContext();

        RecoverabilityAction.MoveToError("error").GetRoutingContexts(context);

        var entry = provider.Entries.Single();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(entry.Exception, Is.SameAs(context.Exception));
            Assert.That(entry.Message, Does.StartWith($"Moving message '{context.MessageId}' to the error queue 'error' because processing failed due to an exception:"));
        }
    }

    [Test]
    public void Should_log_at_the_level_of_the_decision()
    {
        var (context, provider) = CreateContext();

        RecoverabilityAction.ImmediateRetry().GetRoutingContexts(context);
        RecoverabilityAction.DelayedRetry(TimeSpan.FromSeconds(1)).GetRoutingContexts(context);
        RecoverabilityAction.MoveToError("error").GetRoutingContexts(context);
        RecoverabilityAction.Discard("reason").GetRoutingContexts(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(provider.Entries.Select(e => e.Level), Is.EqualTo([LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Information]));
            Assert.That(provider.Entries.Select(e => e.Exception), Is.All.SameAs(context.Exception));
        }
    }

    [Test]
    public void Should_resolve_the_logger_from_the_default_services_of_a_testable_context()
    {
        var context = new TestableRecoverabilityContext();

        Assert.DoesNotThrow(() => RecoverabilityAction.MoveToError("error").GetRoutingContexts(context));
    }

    static (TestableRecoverabilityContext context, CapturingLoggerProvider provider) CreateContext()
    {
        var provider = new CapturingLoggerProvider();
        var context = new TestableRecoverabilityContext { Exception = new InvalidOperationException("boom") };
        context.Services.AddLogging(b => b.AddProvider(provider));
        return (context, provider);
    }
}