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
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input", "input-secondary"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var handlerDispatchedReply = new TaskCompletionSource<ReplyDispatchObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowHandlerToComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            async (messageContext, cancellationToken) =>
            {
                var replyTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input-secondary", headers: new Dictionary<string, string>
                {
                    [Headers.MessageIntent] = MessageIntent.Reply.ToString()
                })), messageContext.TransportTransaction, cancellationToken);
                Assert.That(broker.GetOrCreateQueue("input-secondary").TryPeek(out var replyEnvelope), Is.True);
                handlerDispatchedReply.TrySetResult(new ReplyDispatchObservation(replyTask, replyEnvelope!));
                await allowHandlerToComplete.Task.WaitAsync(cancellationToken);
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        var task = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input")), new TransportTransaction());
        Assert.That(broker.GetOrCreateQueue("input").TryPeek(out var rootEnvelope), Is.True);
        var rootInlineState = InlineExecutionTestHelper.GetInlineState(rootEnvelope!);

        await receiver.StartReceive();

        var observation = await handlerDispatchedReply.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var inlineState = InlineExecutionTestHelper.GetInlineState(observation.Envelope);

        Assert.Multiple(() =>
        {
            Assert.That(inlineState, Is.Not.Null);
            Assert.That(rootInlineState, Is.Not.Null);
            Assert.That(InlineExecutionTestHelper.GetScope(inlineState!), Is.SameAs(InlineExecutionTestHelper.GetScope(rootInlineState!)));
            Assert.That(InlineExecutionTestHelper.GetIsRootDispatch(inlineState!), Is.False);
            Assert.That(observation.Task.IsCompletedSuccessfully, Is.True);
        });

        allowHandlerToComplete.TrySetResult();
        await receiver.StopReceive();
    }

    readonly record struct ReplyDispatchObservation(Task Task, BrokerEnvelope Envelope);
}