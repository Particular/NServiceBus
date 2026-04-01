#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_recoverability_sees_inline_dispatch_context_without_receive_transaction
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await CreateInfrastructure(broker, ["input"], TransportTransactionMode.None);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var observedContext = new TaskCompletionSource<InlineExecutionDispatchContext?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observedScope = new TaskCompletionSource<InlineExecutionScope?>(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) => throw new InvalidOperationException("boom"),
            (errorContext, _) =>
            {
                observedContext.TrySetResult(GetInlineDispatchContext(errorContext.Extensions));
                observedScope.TrySetResult(GetInlineScope(errorContext.TransportTransaction));
                return Task.FromResult(ErrorHandleResult.Handled);
            },
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input")), new TransportTransaction());

        var dispatchContext = await observedContext.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var scope = await observedScope.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.That(async () => await rootTask.WaitAsync(TimeSpan.FromSeconds(5)), Throws.TypeOf<InvalidOperationException>());

        Assert.That(dispatchContext, Is.Not.Null);
        Assert.That(scope, Is.Not.Null);
        Assert.That(GetInlineDispatchDepth(dispatchContext!), Is.EqualTo(0));
        Assert.That(GetInlineDispatchScope(dispatchContext!), Is.SameAs(scope));

        await receiver.StopReceive();
    }
}
