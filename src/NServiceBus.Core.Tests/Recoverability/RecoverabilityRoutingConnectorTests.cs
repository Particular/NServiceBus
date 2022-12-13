namespace NServiceBus.Core.Tests.Recoverability;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Testing;

[TestFixture]
public class RecoverabilityRoutingConnectorTests
{
    [Test]
    public async Task Should_prevent_recoverability_action_changes_after_invoke()
    {
        var connector = new RecoverabilityRoutingConnector(new Notification<MessageToBeRetried>(), new Notification<MessageFaulted>());
        var context = new TestableRecoverabilityContext();

        await connector.Invoke(context, _ => Task.CompletedTask);

        Assert.IsTrue(context.IsLocked);
    }

    [Test]
    public async Task Should_fallback_to_move_to_error_when_total_number_of_retries_exceeds_default_limit()
    {
        var connector = new RecoverabilityRoutingConnector(new Notification<MessageToBeRetried>(), new Notification<MessageFaulted>());
        var context = new TestableRecoverabilityContext()
        {
            ImmediateProcessingFailures = 10,
            DelayedDeliveriesPerformed = 10,
            RecoverabilityAction = new ImmediateRetry(), // custom policy or behavior deviating from the default policy behavior
            RecoverabilityConfiguration = new RecoverabilityConfig(
                new ImmediateConfig(5),
                new DelayedConfig(3, TimeSpan.FromSeconds(0)),
                new FailedConfig("error", new HashSet<Type>(0)))
        };

        await connector.Invoke(context, _ => Task.CompletedTask);

        Assert.IsInstanceOf<MoveToError>(context.RecoverabilityAction);
    }

    [Test]
    public async Task Should_apply_recoverability_action_when_total_number_of_retries_within_custom_limit()
    {
        var connector = new RecoverabilityRoutingConnector(new Notification<MessageToBeRetried>(), new Notification<MessageFaulted>());
        var context = new TestableRecoverabilityContext()
        {
            ImmediateProcessingFailures = 10,
            DelayedDeliveriesPerformed = 5,
            RecoverabilityAction = new ImmediateRetry(), // custom policy or behavior deviating from the default policy behavior
            MaximumRetries = 100, // extend limit
            RecoverabilityConfiguration = new RecoverabilityConfig(
                new ImmediateConfig(5),
                new DelayedConfig(3, TimeSpan.FromSeconds(0)),
                new FailedConfig("error", new HashSet<Type>(0)))
        };

        await connector.Invoke(context, _ => Task.CompletedTask);

        Assert.IsInstanceOf<ImmediateRetry>(context.RecoverabilityAction);
    }

    [Test]
    public async Task Should_fallback_to_move_to_error_when_total_number_of_retries_exceeds_custom_limit()
    {
        var connector = new RecoverabilityRoutingConnector(new Notification<MessageToBeRetried>(), new Notification<MessageFaulted>());
        var context = new TestableRecoverabilityContext()
        {
            ImmediateProcessingFailures = 11, // initial handler invocation also counts towards this
            DelayedDeliveriesPerformed = 10,
            RecoverabilityAction = new ImmediateRetry(), // custom policy or behavior deviating from the default policy behavior
            MaximumRetries = 100, // limit == performed retries
            RecoverabilityConfiguration = new RecoverabilityConfig(
                new ImmediateConfig(5),
                new DelayedConfig(3, TimeSpan.FromSeconds(0)),
                new FailedConfig("error", new HashSet<Type>(0)))
        };

        await connector.Invoke(context, _ => Task.CompletedTask);

        Assert.IsInstanceOf<MoveToError>(context.RecoverabilityAction);
    }
}