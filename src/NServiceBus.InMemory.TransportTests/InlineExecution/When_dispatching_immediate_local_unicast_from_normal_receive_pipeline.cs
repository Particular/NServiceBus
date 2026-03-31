#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_immediate_local_unicast_from_normal_receive_pipeline
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input", "input-secondary"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var handlerDispatchedSend = new TaskCompletionSource<ReplyDispatchObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowHandlerToComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            async (messageContext, cancellationToken) =>
            {
                var sendTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input-secondary", headers: new Dictionary<string, string>
                {
                    [Headers.MessageIntent] = MessageIntent.Send.ToString()
                })), messageContext.TransportTransaction, cancellationToken);
                Assert.That(broker.GetOrCreateQueue("input-secondary").TryPeek(out var sentEnvelope), Is.True);
                handlerDispatchedSend.TrySetResult(new ReplyDispatchObservation(sendTask, sentEnvelope!));
                await allowHandlerToComplete.Task.WaitAsync(cancellationToken);
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await broker.GetOrCreateQueue("input").Enqueue(InlineExecutionTestHelper.CreateReceivedEnvelope("input"));
        await receiver.StartReceive();

        var observation = await handlerDispatchedSend.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var inlineState = InlineExecutionTestHelper.GetInlineState(observation.Envelope);

        Assert.Multiple(() =>
        {
            Assert.That(observation.Task.IsCompletedSuccessfully, Is.True);
            Assert.That(inlineState, Is.Null);
        });

        allowHandlerToComplete.TrySetResult();
        await receiver.StopReceive();
    }

    readonly record struct ReplyDispatchObservation(Task Task, BrokerEnvelope Envelope);
}