#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_nested_inline_immediate_local_sends_reuse_scope
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var parentObserved = new TaskCompletionSource<DispatchObservation>(TaskCreationOptions.RunContinuationsAsynchronously);
        var childObserved = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 2),
            async (messageContext, cancellationToken) =>
            {
                if (messageContext.Headers.TryGetValue("kind", out var kind) && kind == "parent")
                {
                    var sendTask = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input", headers: new Dictionary<string, string>
                    {
                        [Headers.MessageIntent] = MessageIntent.Send.ToString(),
                        ["kind"] = "child"
                    })), messageContext.TransportTransaction, cancellationToken);

                    parentObserved.TrySetResult(new DispatchObservation(sendTask, GetInlineScope(messageContext.TransportTransaction)));
                    return;
                }

                childObserved.TrySetResult(GetInlineScope(messageContext.TransportTransaction));
                await Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await receiver.StartReceive();

        _ = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input", headers: new Dictionary<string, string>
        {
            [Headers.MessageIntent] = MessageIntent.Send.ToString(),
            ["kind"] = "parent"
        })), new TransportTransaction());

        var parent = await parentObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var childScope = await childObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(parent.Scope, Is.Not.Null);
            Assert.That(childScope, Is.Not.Null);
            Assert.That(childScope, Is.SameAs(parent.Scope));
        });

        await receiver.StopReceive();
    }

    readonly record struct DispatchObservation(Task Task, object? Scope);
}