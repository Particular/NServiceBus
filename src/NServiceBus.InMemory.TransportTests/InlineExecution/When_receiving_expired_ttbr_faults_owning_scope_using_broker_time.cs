#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_expired_ttbr_faults_owning_scope_using_broker_time
{
    [Test]
    public async Task Run()
    {
        var fakeTime = CreateFakeTimeProvider();
        await using var broker = new InMemoryBroker(new InMemoryBrokerOptions
        {
            TimeProvider = fakeTime
        });
        var infrastructure = await CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) => Task.CompletedTask,
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        var rootTask = dispatcher.Dispatch(
            new TransportOperations(CreateUnicast("input", discardAfter: TimeSpan.FromSeconds(30))),
            new TransportTransaction());

        fakeTime.Advance(TimeSpan.FromMinutes(1));

        await receiver.StartReceive();

        Assert.That(async () => await rootTask.WaitAsync(TimeSpan.FromSeconds(5)), Throws.Exception);

        await receiver.StopReceive();
    }
}