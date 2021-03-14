namespace NServiceBus.AcceptanceTests.Core.Hosting
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_custom_host_id_is_configured : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_apply_default_to_be_FIPS_compliant()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MyEndpoint>(builder => builder.CustomConfig((endpointConfig, ctx) =>
                {
                    endpointConfig.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(ctx.CustomHostId);
                }))
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.False(context.HostIdDefaultApplied);
            Assert.AreEqual(context.CustomHostId, context.HostIdObserved);
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class MyFeatureThatOverridesHostInformationDefaults : Feature
        {
            public MyFeatureThatOverridesHostInformationDefaults()
            {
                EnableByDefault();
                Defaults(s =>
                {
                    var fieldInfo = s.GetType().GetField("Defaults", BindingFlags.Instance | BindingFlags.NonPublic);
                    var defaults = (ConcurrentDictionary<string, object>)fieldInfo.GetValue(s);

                    var testContext = s.Get<Context>();

                    testContext.HostIdDefaultApplied = defaults.ContainsKey("NServiceBus.HostInformation.HostId");
                    testContext.HostIdObserved = s.Get<Guid>("NServiceBus.HostInformation.HostId");
                });
            }

            protected override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        public class Context : ScenarioContext
        {
            public bool HostIdDefaultApplied { get; set; }
            public Guid CustomHostId { get; } = Guid.NewGuid();
            public Guid HostIdObserved { get; set; }
        }
    }
}