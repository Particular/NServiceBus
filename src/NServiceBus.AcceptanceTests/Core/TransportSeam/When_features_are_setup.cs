namespace NServiceBus.AcceptanceTests.Core.TransportSeam
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using FakeTransport;
    using Features;
    using NUnit.Framework;

    public class When_features_are_setup : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_provide_feature_state_to_transport_initialization()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(c => c.CustomConfig(ec =>
                {
                    ec.DisableFeature<Endpoint.FeatureEnabledByDefaultButDisabledByUser>();
                    ec.EnableFeature<Endpoint.FeatureEnabledByUser>();

                    var fakeTransport = new FakeTransport();
                    fakeTransport.AssertSettings(s =>
                    {
                        Assert.True(s.IsFeatureEnabled(typeof(Endpoint.FeatureEnabledByDefault)));
                        Assert.True(s.IsFeatureEnabled(typeof(Endpoint.FeatureEnabledByUser)));
                        Assert.False(s.IsFeatureEnabled(typeof(Endpoint.FeatureEnabledByDefaultButDisabledByUser)));

                        // this should be "true" but documents that the "active" state can't be used by transports
                        // since Features are activated after the transport have been initialized
                        Assert.False(s.IsFeatureActive(typeof(Endpoint.FeatureEnabledByDefault)));
                    });
                    ec.UseTransport(fakeTransport);
                }))
                .Done(c => c.EndpointsStarted)
                .Run();
        }

        class Context : ScenarioContext
        {
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class FeatureEnabledByDefault : Feature
            {
                public FeatureEnabledByDefault()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                }
            }

            public class FeatureEnabledByUser : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                }
            }

            public class FeatureEnabledByDefaultButDisabledByUser : Feature
            {
                public FeatureEnabledByDefaultButDisabledByUser()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                }
            }
        }
    }
}
