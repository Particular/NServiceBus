namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvanceExtensibility;
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
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Repeat(r => r.For<AllTransportsWithoutNativeDeferral>())
                .Should(c => Assert.IsTrue(c.EndpointsStarted))
                .Run();
        }

        [Test]
        public async Task Endpoint_should_not_shutdown()
        {
            var stopTime = DateTime.UtcNow.AddSeconds(6);

            var testContext =
                await Scenario.Define<TimeoutTestContext>(c => { c.SecondsToWait = 3; })
                    .WithEndpoint<Endpoint>(b =>
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
                    .Done(c => c.FatalErrorOccurred || stopTime <= DateTime.UtcNow)
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

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.GetSettings().Set("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver", TimeSpan.FromSeconds(7));
                    config.EnableFeature<TimeoutManager>();
                });
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