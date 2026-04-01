#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_ordinary_in_pipeline_immediate_local_sends_reuse_inline_scope
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input", "input-secondary"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var secondaryReceiver = infrastructure.Receivers["receiver-1"];
        var handlerDispatchedSend = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        var childHandledScope = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowHandlerToComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            async (messageContext, cancellationToken) =>
            {
                var sendTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input-secondary", headers: new Dictionary<string, string>
                {
                    [Headers.MessageIntent] = MessageIntent.Send.ToString()
                })), messageContext.TransportTransaction, cancellationToken);
                handlerDispatchedSend.TrySetResult(sendTask);
                await allowHandlerToComplete.Task.WaitAsync(cancellationToken);
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await secondaryReceiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (messageContext, _) =>
            {
                childHandledScope.TrySetResult(InlineExecutionTestHelper.GetInlineScope(messageContext.TransportTransaction));
                return Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        var task = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input")), new TransportTransaction());
        Assert.That(broker.GetOrCreateQueue("input").TryPeek(out var rootEnvelope), Is.True);
        var rootInlineState = InlineExecutionTestHelper.GetInlineState(rootEnvelope!);

        await receiver.StartReceive();
        await secondaryReceiver.StartReceive();

        var sendTask = await handlerDispatchedSend.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var childScope = await childHandledScope.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(rootInlineState, Is.Not.Null);
            Assert.That(sendTask.IsCompletedSuccessfully, Is.True);
            Assert.That(childScope, Is.Not.Null);
            Assert.That(childScope, Is.SameAs(InlineExecutionTestHelper.GetScope(rootInlineState!)));
        });

        allowHandlerToComplete.TrySetResult();
        await task.WaitAsync(TimeSpan.FromSeconds(5));
        await receiver.StopReceive();
        await secondaryReceiver.StopReceive();
    }
}
