#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_receiving_shutdown_faults_active_scopes_and_drains_them
{
    [Test]
    public async Task Run()
    {
        await using var broker = new InMemoryBroker();
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;
        var receiver = infrastructure.Receivers["receiver-0"];

        await receiver.Initialize(
            new PushRuntimeSettings(maxConcurrency: 1),
            (_, _) => Task.CompletedTask,
            (_, _) => Task.FromResult(ErrorHandleResult.Handled),
            CancellationToken.None);

        await receiver.StartReceive();

        var rootTask = dispatcher.Dispatch(
            new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input", delay: TimeSpan.FromMinutes(1))),
            new TransportTransaction());

        await receiver.StopReceive(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(rootTask.IsCompleted, Is.True);
            Assert.That(rootTask.IsFaulted, Is.True);
        });
    }
}