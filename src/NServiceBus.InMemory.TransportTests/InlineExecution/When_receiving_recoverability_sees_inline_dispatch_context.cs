#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_recoverability_sees_inline_dispatch_context
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var observedContext = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observedScope = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) => throw new InvalidOperationException("boom"),
            (errorContext, _) =>
            {
                observedContext.TrySetResult(InlineExecutionTestHelper.GetInlineDispatchContext(errorContext.Extensions));
                observedScope.TrySetResult(InlineExecutionTestHelper.GetInlineScope(errorContext.TransportTransaction));
                return Task.FromResult(ErrorHandleResult.Handled);
            },
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input")), new TransportTransaction());

        var dispatchContext = await observedContext.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var scope = await observedScope.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(async () => await rootTask.WaitAsync(TimeSpan.FromSeconds(5)), Throws.TypeOf<InvalidOperationException>());

        Assert.Multiple(() =>
        {
            Assert.That(dispatchContext, Is.Not.Null);
            Assert.That(scope, Is.Not.Null);
            if (dispatchContext != null)
            {
                Assert.That(InlineExecutionTestHelper.GetInlineDispatchDepth(dispatchContext), Is.EqualTo(0));
                Assert.That(InlineExecutionTestHelper.GetInlineDispatchScope(dispatchContext), Is.SameAs(scope));
            }
        });

        await receiver.StopReceive();
    }
}