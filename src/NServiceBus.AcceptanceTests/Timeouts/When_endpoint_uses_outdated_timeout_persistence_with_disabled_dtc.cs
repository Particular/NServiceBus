namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    class When_endpoint_uses_outdated_timeout_persistence_with_disabled_dtc : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_not_start_and_show_warning()
        {
            var context = new Context();
            var scenarioException = Assert.Throws<AggregateException>(() => Scenario.Define(context)
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run())
                .InnerException as ScenarioException;

            Assert.IsFalse(context.EndpointsStarted);
            Assert.IsNotNull(scenarioException);
            StringAssert.Contains("You are using an outdated timeout persistence which can lead to message loss!", scenarioException.InnerException.Message);
        }

        [Test]
        public void Endpoint_should_start_when_warning_suppressed()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Endpoint>(c => c.CustomConfig(b => b.SuppressOutdatedTimeoutPersistenceWarning()))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.EndpointsStarted);
        }


        public class Context : ScenarioContext { }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.Transactions().DisableDistributedTransactions();
                });
            }
        }

        public class Initalizer : Feature
        {
            public Initalizer()
            {
                EnableByDefault();
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                context.Container.ConfigureComponent<OutdatedTimeoutPersister>(DependencyLifecycle.SingleInstance);
            }
        }
    }
}
