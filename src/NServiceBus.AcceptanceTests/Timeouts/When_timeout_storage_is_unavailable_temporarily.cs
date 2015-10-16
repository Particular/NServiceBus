namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using Timeout.Core;
    using NUnit.Framework;

    class When_timeout_storage_is_unavailable_temporarily : NServiceBusAcceptanceTest
    {
        [Test]
        public void Endpoint_should_start()
        {
            var context = new TestContext();

            Scenario.Define(context)
                .WithEndpoint<EndpointWithFlakyTimeoutPersister>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.EndpointsStarted);
        }


        [Test]
        public void Endpoint_should_not_shutdown()
        {
            var context = new TestContext{SecondsToWait = 10};
            var stopTime = DateTime.Now.AddSeconds(45);

            Scenario.Define(context)
                .AllowExceptions(ex => ex.Message.Contains("Persister is temporarily unavailable"))
                .WithEndpoint<EndpointWithFlakyTimeoutPersister>(b =>
                {
                    b.CustomConfig(busConfig =>
                    {
                        busConfig.DefineCriticalErrorAction((s, ex) =>
                        {
                            context.FatalErrorOccurred = true;
                        });
                    });
                })
                .Done(c => context.FatalErrorOccurred || stopTime <= DateTime.Now)
                .Run();

            Assert.IsFalse(context.FatalErrorOccurred, "Circuit breaker was trigged too soon.");
        }

        public class TestContext : ScenarioContext
        {
            public int SecondsToWait { get; set; }
            public bool FatalErrorOccurred { get; set; }
        }

        [Serializable]
        public class MyMessage : IMessage { }

        public class EndpointWithFlakyTimeoutPersister : EndpointConfigurationBuilder
        {
            public TestContext TestContext { get; set; }
            public EndpointWithFlakyTimeoutPersister()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.Transactions().DisableDistributedTransactions();
                    config.SuppressOutdatedTimeoutPersistenceWarning();
                });
            }

            class Initalizer : Feature
            {
                public Initalizer()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.Container
                        .ConfigureComponent<TemporarilyUnavailableTimeoutPersister>(DependencyLifecycle.SingleInstance)
                        .ConfigureProperty(tp => tp.SecondsToWait, 10);
                }
            }
        }
    }
}
