namespace NServiceBus.AcceptanceTests.Core.ScenarioTimeout;

using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

[CancelAfter(5_000)]
public class When_the_test_cancellation_token_is_passed_to_run : NServiceBusAcceptanceTest
{
    [Test, CancelAfter(250)]
    public async Task Should_use_the_passed_token(CancellationToken cancellationToken = default)
    {
        var exception = await Assert.ThrowsAsync<TimeoutException>(() => Scenario.Define<Context>()
            .Run(cancellationToken));

        Assert.That(exception.Message, Does.Contain("timeout budget of 250 ms"));
    }

    public class Context : ScenarioContext;
}
