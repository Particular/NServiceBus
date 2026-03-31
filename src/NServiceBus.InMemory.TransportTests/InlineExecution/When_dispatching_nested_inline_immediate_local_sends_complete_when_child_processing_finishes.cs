#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_nested_inline_immediate_local_sends_complete_when_child_processing_finishes
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var parentObserved = new TaskCompletionSource<DispatchObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var childProcessed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 2),
            async (messageContext, cancellationToken) =>
            {
                if (messageContext.Headers.TryGetValue("kind", out var kind) && kind == "parent")
                {
                    var sendTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input", headers: new Dictionary<string, string>
                    {
                        [Headers.MessageIntent] = MessageIntent.Send.ToString(),
                        ["kind"] = "child"
                    })), messageContext.TransportTransaction, cancellationToken);

                    parentObserved.TrySetResult(new DispatchObservation(sendTask, InlineExecutionTestHelper.GetInlineScope(messageContext.TransportTransaction)));
                    return;
                }

                childProcessed.TrySetResult();
                await Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input", headers: new Dictionary<string, string>
        {
            [Headers.MessageIntent] = MessageIntent.Send.ToString(),
            ["kind"] = "parent"
        })), new TransportTransaction());

        var parent = await parentObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await childProcessed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(parent.Scope, Is.Not.Null);
            Assert.That(parent.Task.IsCompletedSuccessfully, Is.True);
            Assert.That(rootTask.IsCompleted, Is.False);
        });

        await receiver.StopReceive();
    }

    readonly record struct DispatchObservation(Task Task, object? Scope);
}