namespace NServiceBus.AcceptanceTests.Core.ScenarioTimeout;

using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_an_already_cancelled_token_is_passed_to_run : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_fail_with_a_timeout()
    {
        var exception = await Assert.ThrowsAsync<TimeoutException>(() => Scenario.Define<Context>()
            .Run(new CancellationToken(canceled: true)));

        Assert.That(exception.Message, Does.Contain("The scenario did not complete before the cancellation token was cancelled"));
    }

    public class Context : ScenarioContext;
}
