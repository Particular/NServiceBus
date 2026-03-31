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
public class When_receiving_cross_pump_delayed_recoverability_preserves_scope
{
    [Test]
    public async Task Run()
    {
        var fakeTime = InlineExecutionTestHelper.CreateFakeTimeProvider();
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = fakeTime
        });
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input", "input-secondary"]);
        var dispatcher = infrastructure.Dispatcher;
        var primaryReceiver = infrastructure.Receivers["receiver-0"];
        var secondaryReceiver = infrastructure.Receivers["receiver-1"];
        var delayedRetryScheduled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAttemptObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await primaryReceiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) => throw new InvalidOperationException("boom"),
            async (errorContext, cancellationToken) =>
            {
                var action = RecoverabilityAction.DelayedRetry(TimeSpan.FromMinutes(1));
                InlineExecutionTestHelper.SetRecoverabilityAction(errorContext, action);
                await InlineExecutionTestHelper.DispatchRecoverabilityMessage(dispatcher, errorContext, "input-secondary", new DispatchProperties
                {
                    DelayDeliveryWith = new DelayDeliveryWith(TimeSpan.FromMinutes(1))
                }, cancellationToken);
                delayedRetryScheduled.TrySetResult();
                return ErrorHandleResult.Handled;
            },
            CancellationToken.None);

        await secondaryReceiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) =>
            {
                secondAttemptObserved.TrySetResult();
                return Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await primaryReceiver.StartReceive();
        await secondaryReceiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input")), new TransportTransaction());

        await delayedRetryScheduled.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(broker.TryDequeueDelayed(fakeTime.GetUtcNow() + TimeSpan.FromMinutes(2), out var delayedEnvelope), Is.True);

        Assert.That(rootTask.IsCompleted, Is.False);

        await primaryReceiver.StopReceive(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(rootTask.IsCompleted, Is.False);

        broker.EnqueueDelayed(delayedEnvelope!, fakeTime.GetUtcNow());

        await secondAttemptObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await rootTask.WaitAsync(TimeSpan.FromSeconds(5));

        await secondaryReceiver.StopReceive();
    }
}