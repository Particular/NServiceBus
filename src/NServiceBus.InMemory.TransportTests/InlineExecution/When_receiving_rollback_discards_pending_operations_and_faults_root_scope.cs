#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_rollback_discards_pending_operations_and_faults_root_scope
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            async (messageContext, cancellationToken) =>
            {
                await dispatcher.Dispatch(
                    new TransportOperations(CreateUnicast("remote", DispatchConsistency.Default)),
                    messageContext.TransportTransaction,
                    cancellationToken);

                throw new InvalidOperationException("boom");
            },
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(new TransportOperations(CreateUnicast("input")), new TransportTransaction());

        Assert.That(async () => await rootTask.WaitAsync(TimeSpan.FromSeconds(5)), Throws.TypeOf<InvalidOperationException>());
        Assert.That(broker.GetOrCreateQueue("remote").Count, Is.EqualTo(0));

        await receiver.StopReceive();
    }
}