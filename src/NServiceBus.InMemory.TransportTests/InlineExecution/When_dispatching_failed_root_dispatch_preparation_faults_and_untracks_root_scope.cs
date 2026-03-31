#nullable enable

namespace NServiceBus.TransportTests;

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Transport;

[TestFixture]
public class When_dispatching_failed_root_dispatch_preparation_faults_and_untracks_root_scope
{
    [Test]
    public async Task Run()
    {
        var options = new InMemoryBrokerOptions();
        options.ForQueue("input").Send.Mode = InMemorySimulationMode.Reject;
        options.ForQueue("input").Send.RateLimiter = new ScriptedRateLimiter(
            ScriptedRateLimiterStep.Rejected(TimeSpan.FromMinutes(1)));

        await using var broker = new InMemoryBroker(options);
        var infrastructure = await InlineExecutionTestHelper.CreateInfrastructure(broker, ["input"]);
        var dispatcher = infrastructure.Dispatcher;

        var task = dispatcher.Dispatch(new TransportOperations(InlineExecutionTestHelper.CreateUnicast("input")), new TransportTransaction());

        Assert.That(async () => await task.WaitAsync(TimeSpan.FromSeconds(5)), Throws.TypeOf<InMemorySimulationException>());
    }
}