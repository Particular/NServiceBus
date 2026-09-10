namespace NServiceBus.AcceptanceTests.Core.ScenarioTimeout;

using System;
using NServiceBus.AcceptanceTesting;
using NUnit.Framework;

public class When_scenario_exceeds_the_test_timeout : NServiceBusAcceptanceTest
{
    [Test, CancelAfter(250)]
    public void Should_fail_with_the_test_timeout_budget()
    {
        var exception = Assert.ThrowsAsync<TimeoutException>(() => Scenario.Define<Context>()
            .Run());

        Assert.That(exception.Message, Does.Contain("timeout budget of 250 ms"));
    }

    public class Context : ScenarioContext;
}
