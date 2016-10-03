namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    class When_timeout_storage_is_unavailable_temporarily : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Endpoint_should_start()
        {
            return Scenario.Define<TimeoutTestContext>()
                .WithEndpoint<EndpointWithFlakyTimeoutPersister>()
                .Done(c => c.EndpointsStarted)
                .Repeat(r => r.For<AllTransportsWithoutNativeDeferral>())
                .Should(c => Assert.IsTrue(c.EndpointsStarted))
                .Run();
        }

        [Test]
        public async Task Endpoint_should_not_shutdown()
        {
            var stopTime = DateTime.Now.AddSeconds(45);

            var testContext =
                await Scenario.Define<TimeoutTestContext>(c => { c.SecondsToWait = 10; })
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

            Assert.IsFalse(testContext.FatalErrorOccurred, "Circuit breaker was triggered too soon.");
        }

        public class TimeoutTestContext : ScenarioContext
        {
            public int SecondsToWait { get; set; }
            public bool FatalErrorOccurred { get; set; }
        }

        
        public class MyMessage : IMessage
        {
        }

        public class EndpointWithFlakyTimeoutPersister : EndpointConfigurationBuilder
        {
            public EndpointWithFlakyTimeoutPersister()
            {
                EndpointSetup<DefaultServer>(config => { config.EnableFeature<TimeoutManager>(); });
            }

            public TestContext TestContext { get; set; }

            class Initializer : Feature
            {
                public Initializer()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    var testContext = context.Settings.Get<TimeoutTestContext>();
                    context.Container.ConfigureComponent(b => new CyclingOutageTimeoutPersister(testContext.SecondsToWait), DependencyLifecycle.SingleInstance);
                }
            }
        }
    }
}