#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_nested_inline_reentrant_dispatch_returns_child_processing_task
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var nestedSendObserved = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseParentHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var childProcessed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 2),
            async (messageContext, cancellationToken) =>
            {
                if (messageContext.Headers.TryGetValue("kind", out var kind) && kind == "parent")
                {
                    var nestedTask = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input", headers: new Dictionary<string, string>
                    {
                        [Headers.MessageIntent] = MessageIntent.Send.ToString(),
                        ["kind"] = "child"
                    })), messageContext.TransportTransaction, cancellationToken);

                    nestedSendObserved.TrySetResult(nestedTask);
                    await releaseParentHandler.Task.WaitAsync(cancellationToken);
                    return;
                }

                childProcessed.TrySetResult();
                await Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input", headers: new Dictionary<string, string>
        {
            [Headers.MessageIntent] = MessageIntent.Send.ToString(),
            ["kind"] = "parent"
        })), new TransportTransaction());

        var nestedTask = await nestedSendObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await childProcessed.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(nestedTask.IsCompletedSuccessfully, Is.True);
            Assert.That(rootTask.IsCompleted, Is.False);
        });

        releaseParentHandler.TrySetResult();
        await rootTask.WaitAsync(TimeSpan.FromSeconds(5));
        await receiver.StopReceive();
    }
}