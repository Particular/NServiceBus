namespace NServiceBus.AcceptanceTests.Recoverability
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_delayed_retries_enabled_with_no_support : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_on_startup()
        {
            if (TestSuiteConstraints.Current.SupportsDelayedDelivery)
            {
                Assert.Ignore("Ignoring this test because it requires the transport to not support delayed delivery.");
            }

            var exception = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<ScenarioContext>()
                .WithEndpoint<StartedEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run());

            StringAssert.Contains("Delayed retries are not supported when the transport does not support delayed delivery.", exception.ToString());
        }

        public class StartedEndpoint : EndpointConfigurationBuilder
        {
            public StartedEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var recoverability = config.Recoverability();
                    recoverability.Delayed(i => i.NumberOfRetries(1));
                });
            }
        }
    }
}