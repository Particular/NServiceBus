namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;

    class When_timeout_storage_is_unavailable_temporarily : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Endpoint_should_start()
        {
            await Scenario.Define<TimeoutTestContext>()
                .WithEndpoint<EndpointWithFlakyTimeoutPersister>()
                .Done(c => c.EndpointsStarted)
                .Repeat(r => r.For(Transports.Msmq))
                .Should(c => Assert.IsTrue(c.EndpointsStarted))
                .Run();
        }

        [Test]
        public async Task Endpoint_should_not_shutdown()
        {
            var stopTime = DateTime.Now.AddSeconds(45);

            var testContext =
                await Scenario.Define<TimeoutTestContext>(c =>
                    {
                        c.SecondsToWait = 10;
                    })
                    .WithEndpoint<EndpointWithFlakyTimeoutPersister>(b =>
                    {
                        b.CustomConfig((busConfig, context) =>
                        {
                            busConfig.DefineCriticalErrorAction(criticalErrorContext =>
                            {
                                context.FatalErrorOccurred = true;
                                return Task.FromResult(true);
                            });
                        });
                    })
                    .Done(c => c.FatalErrorOccurred || stopTime <= DateTime.Now)
                    .Run();

            Assert.IsFalse(testContext.FatalErrorOccurred, "Circuit breaker was trigged too soon.");
        }

        public class TimeoutTestContext : ScenarioContext
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
                    var testContext = context.Settings.Get<TimeoutTestContext>();
                    context.Container
                        .ConfigureComponent<CyclingOutageTimeoutPersister>(DependencyLifecycle.SingleInstance)
                        .ConfigureProperty(tp => tp.SecondsToWait, testContext.SecondsToWait);
                }
            }
        }
    }
}
