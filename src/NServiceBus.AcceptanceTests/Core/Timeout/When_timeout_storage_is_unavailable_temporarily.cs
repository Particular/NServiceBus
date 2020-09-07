namespace NServiceBus.AcceptanceTests.Core.Timeout
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NServiceBus.Persistence;
    using NServiceBus.Timeout.Core;
    using NUnit.Framework;

    class When_timeout_storage_is_unavailable_temporarily : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Endpoint_should_start()
        {
            Requires.TimeoutStorage();

            var context = await Scenario.Define<TimeoutTestContext>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.IsTrue(context.EndpointsStarted);
        }

        [Test]
        public async Task Endpoint_should_not_shutdown()
        {
            var stopTime = DateTime.UtcNow.AddSeconds(2);

            var testContext =
                await Scenario.Define<TimeoutTestContext>(c => { c.SecondsToWait = 1; })
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
                    config.GetSettings().Set("TimeToWaitBeforeTriggeringCriticalErrorForTimeoutPersisterReceiver", TimeSpan.FromSeconds(3));
                    config.EnableFeature<TimeoutManager>();
                    config.UsePersistence<CustomTimeoutPersister, StorageType.Timeouts>();
                });
            }
        }

        public class CustomTimeoutPersister : PersistenceDefinition
        {
            public CustomTimeoutPersister()
            {
                Supports<StorageType.Timeouts>(s => s.EnableFeatureByDefault<CustomTimeoutPersisterFeature>());
            }

            public class CustomTimeoutPersisterFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    var testContext = context.Settings.Get<TimeoutTestContext>();
                    var persister = new CyclingOutageTimeoutPersister(testContext.SecondsToWait);
                    context.Container.AddSingleton<IPersistTimeouts>(persister);
                    context.Container.AddSingleton<IQueryTimeouts>(persister);
                }
            }
        }
    }
}