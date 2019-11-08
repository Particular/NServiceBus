namespace NServiceBus.AcceptanceTests.Core.Hosting
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
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
                .WithEndpoint<MyEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.False(context.HostIdDefaultApplied);
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(Guid.NewGuid()));
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

                    s.Get<Context>().HostIdDefaultApplied = defaults.ContainsKey("NServiceBus.HostInformation.HostId");
                });
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
            }
        }

        public class Context : ScenarioContext
        {
            public bool HostIdDefaultApplied { get; set; }
        }
    }
}