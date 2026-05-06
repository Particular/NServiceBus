namespace NServiceBus.AcceptanceTests.Core.Recoverability;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NUnit.Framework;

public class When_delayed_retries_enabled_with_no_support : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_throw_when_explicitly_configured()
    {
        if (TestSuiteConstraints.Current.SupportsDelayedDelivery)
        {
            Assert.Ignore("Ignoring this test because it requires the transport to not support delayed delivery.");
        }

        var exception = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<ScenarioContext>()
            .WithEndpoint<EndpointWithExplicitDelayedRetries>()
            .Done(c => c.EndpointsStarted)
            .Run());

        Assert.That(exception.ToString(), Does.Contain("Delayed retries are not supported when the transport does not support delayed delivery."));
    }

    [Test]
    public async Task Should_start_successfully_with_default_delayed_retries()
    {
        if (TestSuiteConstraints.Current.SupportsDelayedDelivery)
        {
            Assert.Ignore("Ignoring this test because it requires the transport to not support delayed delivery.");
        }

        await Scenario.Define<ScenarioContext>()
            .WithEndpoint<EndpointWithDefaultDelayedRetries>()
            .Done(c => c.EndpointsStarted)
            .Run();
    }

    public class EndpointWithExplicitDelayedRetries : EndpointConfigurationBuilder
    {
        public EndpointWithExplicitDelayedRetries() =>
            EndpointSetup<DefaultServer>((config, context) =>
            {
                var recoverability = config.Recoverability();
                recoverability.Delayed(i => i.NumberOfRetries(1));
            });
    }

    public class EndpointWithDefaultDelayedRetries : EndpointConfigurationBuilder
    {
        public EndpointWithDefaultDelayedRetries() =>
            EndpointSetup<DefaultServer>();
    }
}
