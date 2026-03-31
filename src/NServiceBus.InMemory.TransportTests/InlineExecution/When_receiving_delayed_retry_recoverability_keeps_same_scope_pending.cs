#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using DelayedDelivery;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_delayed_retry_recoverability_keeps_same_scope_pending
{
    [Test]
    public async Task Run()
    {
        var fakeTime = InlineExecutionTestHelper.CreateFakeTimeProvider();
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = fakeTime
        });
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var delayedRetryScheduled = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAttemptObserved = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        object? firstScope = null;
        object? secondScope = null;
        var attempts = 0;

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (messageContext, _) =>
            {
                var currentAttempt = Interlocked.Increment(ref attempts);
                var scope = InlineExecutionTestHelper.GetInlineScope(messageContext.TransportTransaction);

                if (currentAttempt == 1)
                {
                    firstScope = scope;
                    throw new InvalidOperationException("boom");
                }

                secondScope = scope;
                secondAttemptObserved.TrySetResult(null);
                return Task.CompletedTask;
            },
            async (errorContext, cancellationToken) =>
            {
                var action = RecoverabilityAction.DelayedRetry(TimeSpan.FromMinutes(1));
                InlineExecutionTestHelper.SetRecoverabilityAction(errorContext, action);
                await InlineExecutionTestHelper.DispatchRecoverabilityMessage(dispatcher, errorContext, new DispatchProperties
                {
                    DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromMinutes(1))
                }, cancellationToken);
                delayedRetryScheduled.TrySetResult(null);
                return ErrorHandleResult.Handled;
            },
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input")), new TransportTransaction());

        await delayedRetryScheduled.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(rootTask.IsCompleted, Is.False);
        Assert.That(firstScope, Is.Not.Null);
        Assert.That(InlineExecutionTestHelper.GetPendingOperations(firstScope!), Is.EqualTo(1));

        Assert.That(broker.TryDequeueDelayed(fakeTime.GetUtcNow() + TimeSpan.FromMinutes(2), out var delayedEnvelope), Is.True);
        Assert.That(InlineExecutionTestHelper.GetInlineState(delayedEnvelope!), Is.Not.Null);
        Assert.That(InlineExecutionTestHelper.GetInlineScope(InlineExecutionTestHelper.GetInlineState(delayedEnvelope!)!), Is.SameAs(firstScope));

        broker.EnqueueDelayed(delayedEnvelope!, delayedEnvelope!.DeliverAt!.Value);
        fakeTime.Advance(TimeSpan.FromMinutes(2));

        await secondAttemptObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await rootTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(secondScope, Is.SameAs(firstScope));

        await receiver.StopReceive();
    }
}