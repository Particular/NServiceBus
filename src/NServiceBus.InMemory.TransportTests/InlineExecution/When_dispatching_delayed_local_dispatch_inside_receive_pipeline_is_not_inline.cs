#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_delayed_local_dispatch_inside_receive_pipeline_is_not_inline
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input", "input-secondary"], TransportTransactionMode.None);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var handlerDispatchedDelayedSend = new TaskCompletionSource<ReplyDispatchObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowHandlerToComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            async (messageContext, cancellationToken) =>
            {
                var sendTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input-secondary", delay: TimeSpan.FromMinutes(1), headers: new Dictionary<string, string>
                {
                    [Headers.MessageIntent] = MessageIntent.Send.ToString()
                })), messageContext.TransportTransaction, cancellationToken);

                Assert.That(broker.TryDequeueDelayed(DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2), out var delayedEnvelope), Is.True);
                handlerDispatchedDelayedSend.TrySetResult(new ReplyDispatchObservation(sendTask, delayedEnvelope!));
                await allowHandlerToComplete.Task.WaitAsync(cancellationToken);
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await broker.GetOrCreateQueue("input").Enqueue(InlineExecutionTestHelper.CreateReceivedEnvelope("input"));
        await receiver.StartReceive();

        var observation = await handlerDispatchedDelayedSend.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(observation.Task.IsCompletedSuccessfully, Is.True);
            Assert.That(InlineExecutionTestHelper.GetInlineState(observation.Envelope), Is.Null);
        });

        allowHandlerToComplete.TrySetResult();
        await receiver.StopReceive();
    }

    readonly record struct ReplyDispatchObservation(Task Task, BrokerEnvelope Envelope);
}