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

    public class When_feature_overrides_hostid_from_feature_default : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task MD5_should_not_be_used()
        {
            var context = await Scenario.Define<Context>(c => { c.CustomIdToApplyInFeature = Guid.NewGuid(); })
                .WithEndpoint<MyEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();

            Assert.False(context.HostIdDefaultPresentWhenFeatureDefaultsAreApplied);
            Assert.AreEqual(context.CustomIdToApplyInFeature, context.HostIdExposedToFeatureSetup);
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                //Configuring a value up front prevents the default to be applied, this can be anything, we will override it later
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
                    // remove the override, we need to hack it via reflection!
                    var fieldInfo = s.GetType().GetField("Overrides", BindingFlags.Instance | BindingFlags.NonPublic);
                    var dictionary = (ConcurrentDictionary<string, object>)fieldInfo.GetValue(s);
                    dictionary.TryRemove("NServiceBus.HostInformation.HostId", out _);

                    // Try to get value, setting should not exist since that means that the default was applied
                    var context = s.Get<Context>();
                    context.HostIdDefaultPresentWhenFeatureDefaultsAreApplied = s.HasSetting("NServiceBus.HostInformation.HostId");

                    // Set override again so we have something
                    s.Set("NServiceBus.HostInformation.HostId", context.CustomIdToApplyInFeature);
                });
            }

            protected override void Setup(FeatureConfigurationContext context)
            {
                context.Settings.Get<Context>().HostIdExposedToFeatureSetup = context.Settings.Get<Guid>("NServiceBus.HostInformation.HostId");
            }
        }

        public class Context : ScenarioContext
        {
            public bool HostIdDefaultPresentWhenFeatureDefaultsAreApplied { get; set; }

            public Guid HostIdExposedToFeatureSetup { get; set; }

            public Guid CustomIdToApplyInFeature { get; set; }
        }
    }
}