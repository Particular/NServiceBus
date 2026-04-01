#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_active_scope_is_placed_in_transport_transaction
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];
        var observedScope = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (messageContext, _) =>
            {
                observedScope.TrySetResult(GetInlineScope(messageContext.TransportTransaction));
                return Task.CompletedTask;
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input")), new TransportTransaction());
        var scope = await observedScope.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await rootTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.That(scope, Is.Not.Null);

        await receiver.StopReceive();
    }
}