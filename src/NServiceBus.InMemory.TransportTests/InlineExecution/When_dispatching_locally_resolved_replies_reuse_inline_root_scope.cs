#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_locally_resolved_replies_reuse_inline_root_scope
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await CreateInfrastructure(broker, ["input", "input-secondary"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var secondaryReceiver = infrastructure.Receivers["receiver-1"];
        var handlerDispatchedReply = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        var replyHandledScope = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowHandlerToComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            async (messageContext, cancellationToken) =>
            {
                var replyTask = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input-secondary", headers: new Dictionary<string, string>
                {
                    [Headers.MessageIntent] = MessageIntent.Reply.ToString()
                })), messageContext.TransportTransaction, cancellationToken);
                handlerDispatchedReply.TrySetResult(replyTask);
                await allowHandlerToComplete.Task.WaitAsync(cancellationToken);
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await secondaryReceiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (messageContext, _) =>
            {
                replyHandledScope.TrySetResult(GetInlineScope(messageContext.TransportTransaction));
                return Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        var task = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input")), new TransportTransaction());
        Assert.That(broker.GetOrCreateQueue("input").TryPeek(out var rootEnvelope), Is.True);
        var rootInlineState = GetInlineState(rootEnvelope!);

        await receiver.StartReceive();
        await secondaryReceiver.StartReceive();

        var replyTask = await handlerDispatchedReply.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var childScope = await replyHandledScope.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(rootInlineState, Is.Not.Null);
            Assert.That(childScope, Is.Not.Null);
            Assert.That(childScope, Is.SameAs(GetInlineScope(rootInlineState!)));
            Assert.That(replyTask.IsCompletedSuccessfully, Is.True);
        });

        allowHandlerToComplete.TrySetResult();
        await task.WaitAsync(TimeSpan.FromSeconds(5));
        await receiver.StopReceive();
        await secondaryReceiver.StopReceive();
    }
}
