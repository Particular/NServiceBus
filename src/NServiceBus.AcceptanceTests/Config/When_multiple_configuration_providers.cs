namespace NServiceBus.AcceptanceTests.Config
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NServiceBus.Config.ConfigurationSource;
    using NUnit.Framework;

    public class When_multiple_configuration_providers : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_at_startup()
        {
            var exception = Assert.ThrowsAsync<Exception>(() => Scenario.Define<ScenarioContext>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.EndpointsStarted)
                .Run());

            Assert.That(exception?.Message, Does.Contain("Multiple configuration providers implementing IProvideConfiguration<CustomConfigSection> were found"));
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(e => e
                    .EnableFeature<CustomFeature>());
            }
        }

        class CustomFeature : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                context.Settings.GetConfigSection<CustomConfigSection>();
            }
        }

        public class ConfigProvider1 : IProvideConfiguration<CustomConfigSection>
        {
            public CustomConfigSection GetConfiguration()
            {
                return new CustomConfigSection();
            }
        }

        public class ConfigProvider2 : IProvideConfiguration<CustomConfigSection>
        {
            public CustomConfigSection GetConfiguration()
            {
                return new CustomConfigSection();
            }
        }

        public class CustomConfigSection
        {
        }
    }
}