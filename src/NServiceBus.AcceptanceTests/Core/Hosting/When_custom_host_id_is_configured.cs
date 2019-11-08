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
                    // remove the override so we can check if the default is set, we need to hack it via reflection!
                    var fieldInfo = s.GetType().GetField("Overrides", BindingFlags.Instance | BindingFlags.NonPublic);
                    var dictionary = (ConcurrentDictionary<string, object>)fieldInfo.GetValue(s);
                    dictionary.TryRemove("NServiceBus.HostInformation.HostId", out _);

                    var context = s.Get<Context>();

                    // If the setting exists that means that the default was applied
                    context.HostIdDefaultApplied = s.HasSetting("NServiceBus.HostInformation.HostId");

                    // Set host id again so the endpoint won't blow up
                    s.Set("NServiceBus.HostInformation.HostId", Guid.NewGuid());
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