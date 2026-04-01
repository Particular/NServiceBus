#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_max_concurrency_1_does_not_deadlock_when_handler_sends_local_child
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var transport = new InMemoryTransport(new InMemoryTransportOptions(broker) { InlineExecution = new() });
        var infrastructure = await transport.Initialize(
            new HostSettings("endpoint", string.Empty, new StartupDiagnosticEntries(), static (_, _, _) => { }, true),
            [new ReceiveSettings("receiver", new QueueAddress("input"), true, true, "error")],
            ["error"],
            CancellationToken.None);

        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver"];
        var processedChild = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var processingFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            async (messageContext, cancellationToken) =>
            {
                if (messageContext.Headers.TryGetValue("kind", out var kind) && kind == "parent")
                {
                    _ = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input", headers: new Dictionary<string, string>
                    {
                        [Headers.MessageIntent] = MessageIntent.Send.ToString(),
                        ["kind"] = "child"
                    })), messageContext.TransportTransaction, cancellationToken);
                    return;
                }

                processedChild.TrySetResult();
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input", headers: new Dictionary<string, string>
        {
            [Headers.MessageIntent] = MessageIntent.Send.ToString(),
            ["kind"] = "parent"
        })), new TransportTransaction());

        _ = rootTask.ContinueWith(
            static (task, state) =>
            {
                var completionSource = (TaskCompletionSource)state!;

                if (task.IsCanceled)
                {
                    completionSource.TrySetCanceled();
                    return;
                }

                if (task.IsFaulted)
                {
                    completionSource.TrySetException(task.Exception!.InnerExceptions);
                    return;
                }

                completionSource.TrySetResult();
            },
            processingFinished,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        await processedChild.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await processingFinished.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(rootTask.IsCompletedSuccessfully, Is.True);

        await receiver.StopReceive();
    }
}